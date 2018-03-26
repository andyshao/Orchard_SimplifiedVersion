using System;
using System.Threading.Tasks;
using Owin;

namespace HotplugWeb.Owin {
    /// <summary>
    /// 一种特殊的Owin中间件，它在Owin管道中被执行，并在请求中运行非Owin部分。
    /// </summary>
    public static class HotplugWebMiddleware {
        public static IAppBuilder UseHotplugWeb(this IAppBuilder app) {
            app.Use(async (context, next) => {
                var handler = context.Environment["HotplugWeb.Handler"] as Func<Task>;

                if (handler == null) {
                    throw new ArgumentException("HotplugWeb.Handler 不能为null");
                }
                await handler();
            });

            return app;
        }
    }
}
