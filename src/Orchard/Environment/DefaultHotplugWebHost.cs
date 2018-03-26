using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotplugWeb.Caching;
using HotplugWeb.Environment.Configuration;
using HotplugWeb.Environment.Extensions;
using HotplugWeb.Environment.ShellBuilders;
using HotplugWeb.Environment.State;
using HotplugWeb.Environment.Descriptor;
using HotplugWeb.Environment.Descriptor.Models;
using HotplugWeb.Localization;
using HotplugWeb.Logging;
using HotplugWeb.Mvc;
using HotplugWeb.Mvc.Extensions;
using HotplugWeb.Utility.Extensions;
using HotplugWeb.Utility;
using System.Threading;

namespace HotplugWeb.Environment {
    // DefaultHotplugWebHost ʵ�ֵ������¼�������򶼱����� HotplugWebStarter ��������
    public class DefaultHotplugWebHost : IHotplugWebHost, IShellSettingsManagerEventHandler, IShellDescriptorManagerEventHandler {
        private readonly IHostLocalRestart _hostLocalRestart;
        private readonly IShellSettingsManager _shellSettingsManager;
        private readonly IShellContextFactory _shellContextFactory;
        private readonly IRunningShellTable _runningShellTable;
        private readonly IProcessingEngine _processingEngine;
        private readonly IExtensionLoaderCoordinator _extensionLoaderCoordinator;
        private readonly IExtensionMonitoringCoordinator _extensionMonitoringCoordinator;
        private readonly ICacheManager _cacheManager;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly static object _syncLock = new object();
        private readonly static object _shellContextsWriteLock = new object();
        private readonly NamedReaderWriterLock _shellActivationLock = new NamedReaderWriterLock();

        private IEnumerable<ShellContext> _shellContexts;
        private readonly ContextState<IList<ShellSettings>> _tenantsToRestart;

        public int Retries { get; set; }
        public bool DelayRetries { get; set; }

        public DefaultHotplugWebHost(
            IShellSettingsManager shellSettingsManager,
            IShellContextFactory shellContextFactory,
            IRunningShellTable runningShellTable,
            IProcessingEngine processingEngine,
            IExtensionLoaderCoordinator extensionLoaderCoordinator,
            IExtensionMonitoringCoordinator extensionMonitoringCoordinator,
            ICacheManager cacheManager,
            IHostLocalRestart hostLocalRestart, 
            IHttpContextAccessor httpContextAccessor) {

            _shellSettingsManager = shellSettingsManager;
            _shellContextFactory = shellContextFactory;
            _runningShellTable = runningShellTable;
            _processingEngine = processingEngine;
            _extensionLoaderCoordinator = extensionLoaderCoordinator;
            _extensionMonitoringCoordinator = extensionMonitoringCoordinator;
            _cacheManager = cacheManager;
            _hostLocalRestart = hostLocalRestart;
            _httpContextAccessor = httpContextAccessor;

            _tenantsToRestart = new ContextState<IList<ShellSettings>>("DefaultHotplugWebHost.TenantsToRestart", () => new List<ShellSettings>());

            T = NullLocalizer.Instance;
            Logger = NullLogger.Instance;
        }

        public Localizer T { get; set; }
        public ILogger Logger { get; set; }

        public IList<ShellContext> Current {
            get { return BuildCurrent().ToReadOnlyCollection(); }
        }

        public ShellContext GetShellContext(ShellSettings shellSettings) {
            return BuildCurrent().SingleOrDefault(shellContext => shellContext.Settings.Name.Equals(shellSettings.Name));
        }

        void IHotplugWebHost.Initialize() {
            Logger.Information("��ʼ����");
            BuildCurrent();
            Logger.Information("��ʼ������");
        }

        void IHotplugWebHost.ReloadExtensions() {
            DisposeShellContext();
        }

        void IHotplugWebHost.BeginRequest() {
            Logger.Debug("��ʼ���� ");
            BeginRequest();
        }

        void IHotplugWebHost.EndRequest() {
            Logger.Debug("��������");
            EndRequest();
        }

        IWorkContextScope IHotplugWebHost.CreateStandaloneEnvironment(ShellSettings shellSettings) {
            Logger.Debug("Ϊ�⻧{0}������������", shellSettings.Name);

            MonitorExtensions();
            BuildCurrent();

            var shellContext = CreateShellContext(shellSettings);
            var workContext = shellContext.LifetimeScope.CreateWorkContextScope();
            return new StandaloneEnvironmentWorkContextScopeWrapper(workContext, shellContext);
        }

        /// <summary>
        /// ȷ��shells������, ������չ�Ѹ���ʱ���¼���
        /// </summary>
        IEnumerable<ShellContext> BuildCurrent() {
            if (_shellContexts == null) {
                lock (_syncLock) {
                    if (_shellContexts == null) {
                        SetupExtensions();
                        MonitorExtensions();
                        CreateAndActivateShells();
                    }
                }
            }

            return _shellContexts;
        }

        void StartUpdatedShells() {
            while (_tenantsToRestart.GetState().Any()) {
                var settings = _tenantsToRestart.GetState().First();
                _tenantsToRestart.GetState().Remove(settings);
                Logger.Debug("���� shell: " + settings.Name);
                lock (_syncLock) {
                    ActivateShell(settings);
                }
            }
        }

        void CreateAndActivateShells() {
            Logger.Information("��ʼ���� shells");

            // �������⻧��
            var allSettings = _shellSettingsManager.LoadSettings()
                .Where(settings => settings.State == TenantState.Running || settings.State == TenantState.Uninitialized || settings.State == TenantState.Initializing)
                .ToArray();

            // ���������⻧, ���������ǵ�shell��
            if (allSettings.Any()) {
                Parallel.ForEach(allSettings, settings => {
                    for (var i = 0; i <= Retries; i++) {

                        // ���ǵ�һ�γ���, �ȴ�һ��ʱ��...
                        if (DelayRetries && i > 0) {

                            // �ȴ� i^2, ����ζ�� 1, 2, 4, 8... ��
                            Thread.Sleep(TimeSpan.FromSeconds(Math.Pow(i, 2)));
                        }

                        try {
                            var context = CreateShellContext(settings);
                            ActivateShell(context);

                            // ���һ��˳��, ����ֹͣ����ѭ��
                            break;
                        }
                        catch (Exception ex) {
                            if (i == Retries) {
                                Logger.Fatal("һ���⻧��������:{0}��{1}����֮��", settings.Name, Retries);
                                return;
                            }
                            else {
                                Logger.Error(ex, "�޷������⻧: " + settings.Name + "���Դ���: " + i);
                            }
                        }
                        
                    }

                    while (_processingEngine.AreTasksPending()) {
                        Logger.Debug("�ڼ���Shell�����������");
                        _processingEngine.ExecuteNextTask();
                    }
                });
            }
            // No settings, run the Setup.
            else {
                var setupContext = CreateSetupContext();
                ActivateShell(setupContext);
            }

            Logger.Information("��ɴ��� shells");
        }

        /// <summary>
        /// Starts a Shell and registers its settings in RunningShellTable
        /// </summary>
        private void ActivateShell(ShellContext context) {
            Logger.Debug("�����⻧{0}��������", context.Settings.Name);
            context.Shell.Activate();

            lock (_shellContextsWriteLock) {
                _shellContexts = (_shellContexts ?? Enumerable.Empty<ShellContext>())
                                .Where(c => c.Settings.Name != context.Settings.Name)
                                .Concat(new[] { context })
                                .ToArray();
            }

            _runningShellTable.Add(context.Settings);
        }

        /// <summary>
        /// ΪĬ���⻧�����ô���һ����ʱshell��
        /// </summary>
        private ShellContext CreateSetupContext() {
            Logger.Debug("Ϊroot��װ���� shell ������.");
            return _shellContextFactory.CreateSetupContext(new ShellSettings { Name = ShellSettings.DefaultName });
        }

        /// <summary>
        /// ���� shell ���ô��� shell �����ġ�
        /// </summary>
        private ShellContext CreateShellContext(ShellSettings settings) {
            if (settings.State == TenantState.Uninitialized || settings.State == TenantState.Invalid) {
                Logger.Debug("����Ϊ�⻧ {0} ��װ���򴴽� shell �����ġ�", settings.Name);
                return _shellContextFactory.CreateSetupContext(settings);
            }

            Logger.Debug("����Ϊ�⻧ {0} ���� shell �����ġ�", settings.Name);
            return _shellContextFactory.CreateShellContext(settings);
        }

        private void SetupExtensions() {
            _extensionLoaderCoordinator.SetupExtensions();
        }

        private void MonitorExtensions() {
            // ����һ�����ٵġ�������Ŀ��������չ�������ĵ���Ա֪ͨ����(ͨ����_current��������Ϊ��null��)��
            // ����չ�ڴ����ϸ���ʱ��������Ҫ���¼����µ�/���µ���չ��
            _cacheManager.Get("HotplugWebHost_Extensions", true,
                              ctx => {
                                  _extensionMonitoringCoordinator.MonitorExtensions(ctx.Monitor);
                                  _hostLocalRestart.Monitor(ctx.Monitor);
                                  DisposeShellContext();
                                  return "";
                              });
        }

        /// <summary>
        /// ��ֹ���л��shell�����ģ����������ǵķ�Χ����ʹ�����ڱ�Ҫʱ���¼��ء�
        /// </summary>
        private void DisposeShellContext() {
            Logger.Information("����shell������");

            if (_shellContexts != null) {
                lock (_syncLock) {
                    if (_shellContexts != null) {
                        foreach (var shellContext in _shellContexts) {
                            shellContext.Shell.Terminate();
                            shellContext.Dispose();
                        }
                    }
                }
                _shellContexts = null;
            }
        }

        protected virtual void BeginRequest() {
            BlockRequestsDuringSetup();

            Action ensureInitialized = () => {
                // ȷ������������shell�����ģ����������չ�������ģ�����Ҫ���¼��ء�
                MonitorExtensions();
                BuildCurrent();
            };

            ShellSettings currentShellSettings = null;

            var httpContext = _httpContextAccessor.Current();
            if (httpContext != null) {
                currentShellSettings = _runningShellTable.Match(httpContext);
            }

            if (currentShellSettings == null) {
                ensureInitialized();
            }
            else {
                _shellActivationLock.RunWithReadLock(currentShellSettings.Name, () => {
                    ensureInitialized();
                });
            }

            // StartUpdatedShells ���ܻᵼ�±�д�� shell ������, �����Ӧ���ڶ�������֮�����С�
            StartUpdatedShells();
        }

        protected virtual void EndRequest() {
            // ͬ���������й��������
            // �ڹܵ�����һ���Ͽ��԰�ȫ��ִ�д˲���, 
            // ��Ϊ���������ѹر�, 
            // ���Ϊ��Щ���񴴽��µĻ��������񽫰���Ԥ�ڵķ�ʽ���С�
            while (_processingEngine.AreTasksPending()) {
                Logger.Debug("Processing pending task");
                _processingEngine.ExecuteNextTask();
            }

            StartUpdatedShells();
        }

        void IShellSettingsManagerEventHandler.Saved(ShellSettings settings) {
            Logger.Debug("Shell saved: " + settings.Name);

            // ����Ѵ����⻧
            if (settings.State != TenantState.Invalid) {
                if (!_tenantsToRestart.GetState().Any(t => t.Name.Equals(settings.Name))) {
                    Logger.Debug("Adding tenant to restart: " + settings.Name + " " + settings.State);
                    _tenantsToRestart.GetState().Add(settings);
                }
            }
        }

        public void ActivateShell(ShellSettings settings) {
            Logger.Debug("Activating shell: " + settings.Name);

            // ���ҹ����� shell ������
            var shellContext = _shellContexts.FirstOrDefault(c => c.Settings.Name == settings.Name);

            if (shellContext == null && settings.State == TenantState.Disabled) {
                return;
            }

            // �������⻧�𣿻��ǵȴ���װ���⻧��
            if (shellContext == null || settings.State == TenantState.Uninitialized) {
                // ���� Shell
                var context = CreateShellContext(settings);

                // ���� Shell
                ActivateShell(context);
            }
            // ����⻧������, ����ֹ shell
            else if (settings.State == TenantState.Disabled) {
                shellContext.Shell.Terminate();
                _runningShellTable.Remove(settings);

                // ʹ�� ToArray()ǿ��ö�� ���, ͨ�������ͷŵ�������, ����ִ�в��ᵼ�����⡣
                _shellContexts = _shellContexts.Where(shell => shell.Settings.Name != settings.Name).ToArray();

                shellContext.Dispose();
            }
            // ���¼��� shell, ��Ϊ���������Ѹ���
            else
            {
                _shellActivationLock.RunWithWriteLock(settings.Name, () => {
                    // ������һ��������
                    shellContext.Shell.Terminate();

                    var context = _shellContextFactory.CreateShellContext(settings);

                    // ���ע���޸ĺ�������ġ�
                    // ʹ�� ToArray()ǿ��ö�� ���, ͨ�������ͷŵ�������, ����ִ�в��ᵼ�����⡣
                    _shellContexts = _shellContexts.Where(shell => shell.Settings.Name != settings.Name).Union(new[] { context }).ToArray();

                    shellContext.Dispose();
                    context.Shell.Activate();

                    _runningShellTable.Update(settings);
                });
            }
        }

        /// <summary>
        /// ����������/����, ��Ҫ���������⻧
        /// </summary>
        void IShellDescriptorManagerEventHandler.Changed(ShellDescriptor descriptor, string tenant) {
            if (_shellContexts == null) {
                return;
            }

            Logger.Debug("Shell changed: " + tenant);

            var context = _shellContexts.FirstOrDefault(x => x.Settings.Name == tenant);

            if (context == null) {
                return;
            }

            // �ڰ�װʱ�����������⻧
            if (context.Settings.State != TenantState.Running) {
                return;
            }

            // ������г��⻧, �벻Ҫ������б��
            if (_tenantsToRestart.GetState().Any(x => x.Name == tenant)) {
                return;
            }

            Logger.Debug("Adding tenant to restart: " + tenant);
            _tenantsToRestart.GetState().Add(context.Settings);
        }

        private void BlockRequestsDuringSetup() {
            var httpContext = _httpContextAccessor.Current();
            if (httpContext.IsBackgroundContext())
                return;

            // ��ȡ����� shell��
            var runningShell = _runningShellTable.Match(httpContext);
            if (runningShell == null)
                return;

            // �������� shell ��ǰ���ڳ�ʼ��, �򷵻�һ�����񲻿��õ� HTTP ״̬����.
            if (runningShell.State == TenantState.Initializing) {
                var response = httpContext.Response;
                response.StatusCode = 503;
                response.StatusDescription = "This tenant is currently initializing. Please try again later.";
                response.Write("This tenant is currently initializing. Please try again later.");
            }
        }

        // ���� CreateStandaloneEnvironment(), ���ͷ��� ShellContext LifetimeScope.
        private class StandaloneEnvironmentWorkContextScopeWrapper : IWorkContextScope {
            private readonly ShellContext _shellContext;
            private readonly IWorkContextScope _workContextScope;

            public WorkContext WorkContext {
                get { return _workContextScope.WorkContext; }
            }

            public StandaloneEnvironmentWorkContextScopeWrapper(IWorkContextScope workContextScope, ShellContext shellContext) {
                _workContextScope = workContextScope;
                _shellContext = shellContext;
            }

            public TService Resolve<TService>() {
                return _workContextScope.Resolve<TService>();
            }

            public bool TryResolve<TService>(out TService service) {
                return _workContextScope.TryResolve<TService>(out service);
            }

            public void Dispose() {
                _workContextScope.Dispose();
                _shellContext.Dispose();
            }
        }
    }
}
