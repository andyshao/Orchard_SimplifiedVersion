using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Autofac;
using Orchard.Environment;
using Orchard.WarmupStarter;
using System.Web.Http;

namespace Orchard.Web {
    public class MvcApplication : HttpApplication {
        private static Starter<IOrchardHost> _starter;

        public MvcApplication() {
        }

        public static void RegisterRoutes(RouteCollection routes) {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
        }

        protected void Application_Start() {
            RegisterRoutes(RouteTable.Routes);
            _starter = new Starter<IOrchardHost>(HostInitialization, HostBeginRequest, HostEndRequest);
            _starter.OnApplicationStart(this);
        }

        protected void Application_BeginRequest() {
            _starter.OnBeginRequest(this);
        }

        protected void Application_EndRequest() {
            _starter.OnEndRequest(this);
        }

        private static void HostBeginRequest(HttpApplication application, IOrchardHost host) {
            application.Context.Items["originalHttpContext"] = application.Context;
            host.BeginRequest();
        }

        private static void HostEndRequest(HttpApplication application, IOrchardHost host) {
            host.EndRequest();
        }

        private static IOrchardHost HostInitialization(HttpApplication application) {
            var host = OrchardStarter.CreateHost(MvcSingletons);
            host.Initialize();

            // 初始化shell以加速第一个动态查询。
            host.BeginRequest();
            host.EndRequest();

            return host;
        }

        static void MvcSingletons(ContainerBuilder builder) {
            builder.Register(ctx => RouteTable.Routes).SingleInstance();
            builder.Register(ctx => ModelBinders.Binders).SingleInstance();
            builder.Register(ctx => ViewEngines.Engines).SingleInstance();
        }
    }
}
