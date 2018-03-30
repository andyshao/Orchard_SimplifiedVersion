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
    // DefaultHotplugWebHost 实现的所有事件处理程序都必须在 HotplugWebStarter 中声明。
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
            Logger.Information("初始化中");
            BuildCurrent();
            Logger.Information("初始化结束");
        }

        void IHotplugWebHost.ReloadExtensions() {
            DisposeShellContext();
        }

        void IHotplugWebHost.BeginRequest() {
            Logger.Debug("开始请求 ");
            BeginRequest();
        }

        void IHotplugWebHost.EndRequest() {
            Logger.Debug("结束请求");
            EndRequest();
        }

        IWorkContextScope IHotplugWebHost.CreateStandaloneEnvironment(ShellSettings shellSettings) {
            Logger.Debug("为租户{0}创建独立环境", shellSettings.Name);

            MonitorExtensions();
            BuildCurrent();

            var shellContext = CreateShellContext(shellSettings);
            var workContext = shellContext.LifetimeScope.CreateWorkContextScope();
            return new StandaloneEnvironmentWorkContextScopeWrapper(workContext, shellContext);
        }

        /// <summary>
        /// 确保shells被激活, 或在扩展已更改时重新激活
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
                Logger.Debug("更新 shell: " + settings.Name);
                lock (_syncLock) {
                    ActivateShell(settings);
                }
            }
        }

        void CreateAndActivateShells() {
            Logger.Information("开始创建 shells");

            // 现在有租户吗？
            var allSettings = _shellSettingsManager.LoadSettings()
                .Where(settings => settings.State == TenantState.Running || settings.State == TenantState.Uninitialized || settings.State == TenantState.Initializing)
                .ToArray();

            // 加载所有租户, 并激活他们的shell。
            if (allSettings.Any()) {
                Parallel.ForEach(allSettings, settings => {
                    for (var i = 0; i <= Retries; i++) {

                        // 不是第一次尝试, 等待一段时间...
                        if (DelayRetries && i > 0) {

                            // 等待 i^2, 这意味着 1, 2, 4, 8... 秒
                            Thread.Sleep(TimeSpan.FromSeconds(Math.Pow(i, 2)));
                        }

                        try {
                            var context = CreateShellContext(settings);
                            ActivateShell(context);

                            // 如果一切顺利, 返回停止重试循环
                            break;
                        }
                        catch (Exception ex) {
                            if (i == Retries) {
                                Logger.Fatal("一个租户不能启动:{0}在{1}重试之后。", settings.Name, Retries);
                                return;
                            }
                            else {
                                Logger.Error(ex, "无法启动租户: " + settings.Name + "尝试次数: " + i);
                            }
                        }
                        
                    }

                    while (_processingEngine.AreTasksPending()) {
                        Logger.Debug("在激活Shell后处理待定任务。");
                        _processingEngine.ExecuteNextTask();
                    }
                });
            }
            // No settings, run the Setup.
            else {
                var setupContext = CreateSetupContext();
                ActivateShell(setupContext);
            }

            Logger.Information("完成创建 shells");
        }

        /// <summary>
        /// Starts a Shell and registers its settings in RunningShellTable
        /// </summary>
        private void ActivateShell(ShellContext context) {
            Logger.Debug("激活租户{0}的上下文", context.Settings.Name);
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
        /// 为默认租户的设置创建一个临时shell。
        /// </summary>
        private ShellContext CreateSetupContext() {
            Logger.Debug("为root安装创建 shell 上下文.");
            return _shellContextFactory.CreateSetupContext(new ShellSettings { Name = ShellSettings.DefaultName });
        }

        /// <summary>
        /// 基于 shell 设置创建 shell 上下文。
        /// </summary>
        private ShellContext CreateShellContext(ShellSettings settings) {
            if (settings.State == TenantState.Uninitialized || settings.State == TenantState.Invalid) {
                Logger.Debug("正在为租户 {0} 安装程序创建 shell 上下文。", settings.Name);
                return _shellContextFactory.CreateSetupContext(settings);
            }

            Logger.Debug("正在为租户 {0} 创建 shell 上下文。", settings.Name);
            return _shellContextFactory.CreateShellContext(settings);
        }

        private void SetupExtensions() {
            _extensionLoaderCoordinator.SetupExtensions();
        }

        private void MonitorExtensions() {
            // 这是一个“假的”缓存条目，允许扩展加载器的调度员通知我们(通过将_current重新设置为“null”)，
            // 当扩展在磁盘上更改时，我们需要重新加载新的/更新的扩展。
            _cacheManager.Get("HotplugWebHost_Extensions", true,
                              ctx => {
                                  _extensionMonitoringCoordinator.MonitorExtensions(ctx.Monitor);
                                  _hostLocalRestart.Monitor(ctx.Monitor);
                                  DisposeShellContext();
                                  return "";
                              });
        }

        /// <summary>
        /// 终止所有活动的shell上下文，并处理它们的范围，迫使它们在必要时重新加载。
        /// </summary>
        private void DisposeShellContext() {
            Logger.Information("处理活动shell上下文");

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
                // 确保加载了所有shell上下文，或者如果扩展发生更改，则需要重新加载。
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

            // StartUpdatedShells 可能会导致编写器 shell 激活锁, 因此它应该在读卡器锁之外运行。
            StartUpdatedShells();
        }

        protected virtual void EndRequest() {
            // 同步处理所有挂起的任务。
            // 在管道的这一点上可以安全地执行此操作, 
            // 因为请求事务已关闭, 
            // 因此为这些任务创建新的环境和事务将按照预期的方式运行。
            while (_processingEngine.AreTasksPending()) {
                Logger.Debug("Processing pending task");
                _processingEngine.ExecuteNextTask();
            }

            StartUpdatedShells();
        }

        void IShellSettingsManagerEventHandler.Saved(ShellSettings settings) {
            Logger.Debug("Shell saved: " + settings.Name);

            // 如果已创建租户
            if (settings.State != TenantState.Invalid) {
                if (!_tenantsToRestart.GetState().Any(t => t.Name.Equals(settings.Name))) {
                    Logger.Debug("Adding tenant to restart: " + settings.Name + " " + settings.State);
                    _tenantsToRestart.GetState().Add(settings);
                }
            }
        }

        public void ActivateShell(ShellSettings settings) {
            Logger.Debug("Activating shell: " + settings.Name);

            // 查找关联的 shell 上下文
            var shellContext = _shellContexts.FirstOrDefault(c => c.Settings.Name == settings.Name);

            if (shellContext == null && settings.State == TenantState.Disabled) {
                return;
            }

            // 这是新租户吗？还是等待安装的租户？
            if (shellContext == null || settings.State == TenantState.Uninitialized) {
                // 创建 Shell
                var context = CreateShellContext(settings);

                // 激活 Shell
                ActivateShell(context);
            }
            // 如果租户被禁用, 则终止 shell
            else if (settings.State == TenantState.Disabled) {
                shellContext.Shell.Terminate();
                _runningShellTable.Remove(settings);

                // 使用 ToArray()强制枚举 因此, 通过访问释放的上下文, 惰性执行不会导致问题。
                _shellContexts = _shellContexts.Where(shell => shell.Settings.Name != settings.Name).ToArray();

                shellContext.Dispose();
            }
            // 重新加载 shell, 因为它的设置已更改
            else
            {
                _shellActivationLock.RunWithWriteLock(settings.Name, () => {
                    // 处理上一个上下文
                    shellContext.Shell.Terminate();

                    var context = _shellContextFactory.CreateShellContext(settings);

                    // 激活并注册修改后的上下文。
                    // 使用 ToArray()强制枚举 因此, 通过访问释放的上下文, 惰性执行不会导致问题。
                    _shellContexts = _shellContexts.Where(shell => shell.Settings.Name != settings.Name).Union(new[] { context }).ToArray();

                    shellContext.Dispose();
                    context.Shell.Activate();

                    _runningShellTable.Update(settings);
                });
            }
        }

        /// <summary>
        /// 功能已启用/禁用, 需要重新启动租户
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

            // 在安装时不重新启动租户
            if (context.Settings.State != TenantState.Running) {
                return;
            }

            // 如果已列出租户, 请不要对其进行标记
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

            // 获取请求的 shell。
            var runningShell = _runningShellTable.Match(httpContext);
            if (runningShell == null)
                return;

            // 如果请求的 shell 当前正在初始化, 则返回一个服务不可用的 HTTP 状态代码.
            if (runningShell.State == TenantState.Initializing) {
                var response = httpContext.Response;
                response.StatusCode = 503;
                response.StatusDescription = "This tenant is currently initializing. Please try again later.";
                response.Write("This tenant is currently initializing. Please try again later.");
            }
        }

        // 用于 CreateStandaloneEnvironment(), 还释放了 ShellContext LifetimeScope.
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
