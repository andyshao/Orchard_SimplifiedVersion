using System;
using System.Collections.Generic;
using System.Linq;
using Autofac.Features.OwnedInstances;
using Microsoft.Owin.Builder;
using HotplugWeb.Environment.Configuration;
using HotplugWeb.Logging;
using HotplugWeb.Mvc.ModelBinders;
using HotplugWeb.Mvc.Routes;
using HotplugWeb.Owin;
using HotplugWeb.Tasks;
using HotplugWeb.UI;
using HotplugWeb.WebApi.Routes;
using HotplugWeb.Exceptions;
using IModelBinderProvider = HotplugWeb.Mvc.ModelBinders.IModelBinderProvider;

namespace HotplugWeb.Environment
{
    public class DefaultHotplugWebShell : IHotplugWebShell {
        private readonly IWorkContextAccessor _workContextAccessor;
        private readonly IEnumerable<IRouteProvider> _routeProviders;
        private readonly IEnumerable<IHttpRouteProvider> _httpRouteProviders;
        private readonly IRoutePublisher _routePublisher;
        private readonly IEnumerable<IModelBinderProvider> _modelBinderProviders;
        private readonly IModelBinderPublisher _modelBinderPublisher;
        private readonly ISweepGenerator _sweepGenerator;
        private readonly IEnumerable<IOwinMiddlewareProvider> _owinMiddlewareProviders;
        private readonly ShellSettings _shellSettings;

        public DefaultHotplugWebShell(
            IWorkContextAccessor workContextAccessor,
            IEnumerable<IRouteProvider> routeProviders,
            IEnumerable<IHttpRouteProvider> httpRouteProviders,
            IRoutePublisher routePublisher,
            IEnumerable<IModelBinderProvider> modelBinderProviders,
            IModelBinderPublisher modelBinderPublisher,
            ISweepGenerator sweepGenerator,
            IEnumerable<IOwinMiddlewareProvider> owinMiddlewareProviders,
            ShellSettings shellSettings) {
            _workContextAccessor = workContextAccessor;
            _routeProviders = routeProviders;
            _httpRouteProviders = httpRouteProviders;
            _routePublisher = routePublisher;
            _modelBinderProviders = modelBinderProviders;
            _modelBinderPublisher = modelBinderPublisher;
            _sweepGenerator = sweepGenerator;
            _owinMiddlewareProviders = owinMiddlewareProviders;
            _shellSettings = shellSettings;

            Logger = NullLogger.Instance;
        }

        public ILogger Logger { get; set; }

        public void Activate() {
            var appBuilder = new AppBuilder();
            appBuilder.Properties["host.AppName"] = _shellSettings.Name;

            using (var scope = _workContextAccessor.CreateWorkContextScope()) {
                var orderedMiddlewares = _owinMiddlewareProviders
                    .SelectMany(p => p.GetOwinMiddlewares())
                    .OrderBy(obj => obj.Priority, new FlatPositionComparer());

                foreach (var middleware in orderedMiddlewares) {
                    middleware.Configure(appBuilder);
                }
            }

            // 将HotplugWeb中间件注册到所有其他中间件之后。
            appBuilder.UseHotplugWeb();

            var pipeline = appBuilder.Build();
            var allRoutes = new List<RouteDescriptor>();
            allRoutes.AddRange(_routeProviders.SelectMany(provider => provider.GetRoutes()));
            allRoutes.AddRange(_httpRouteProviders.SelectMany(provider => provider.GetRoutes()));

            _routePublisher.Publish(allRoutes, pipeline);
            _modelBinderPublisher.Publish(_modelBinderProviders.SelectMany(provider => provider.GetModelBinders()));

            using (var scope = _workContextAccessor.CreateWorkContextScope()) {
                using (var events = scope.Resolve<Owned<IHotplugWebShellEvents>>()) {
                    events.Value.Activated();
                }
            }
            
            _sweepGenerator.Activate();
        }

        public void Terminate() {
            SafelyTerminate(() => {
                using (var scope = _workContextAccessor.CreateWorkContextScope()) {
                    using (var events = scope.Resolve<Owned<IHotplugWebShellEvents>>()) {
                        SafelyTerminate(() => events.Value.Terminating());
                    }
                }  
            });

            SafelyTerminate(() => _sweepGenerator.Terminate());
        }

        private void SafelyTerminate(Action action) {
            try {
                action();
            }
            catch(Exception ex) {
                if (ex.IsFatal()) {
                    throw;
                }

                Logger.Error(ex, "在终止Shell时发生意外错误。");
            }
        }
    }
}
