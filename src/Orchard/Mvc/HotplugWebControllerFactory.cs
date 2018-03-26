using System;
using System.Web.Mvc;
using System.Web.Routing;
using Autofac;
using Autofac.Core;
using Autofac.Features.Metadata;
using HotplugWeb.Mvc.Extensions;

namespace HotplugWeb.Mvc {
    /// <summary>
    /// 重写默认控制器工厂以使用LoC解析控制器，基于它们的区域和名称。
    /// </summary>
    public class HotplugWebControllerFactory : DefaultControllerFactory {
        /// <summary>
        /// 尝试解析与工作上下文作用域的给定服务密钥关联的控制器的实例。
        /// </summary>
        /// <typeparam name="T">控制器的类型。</typeparam>
        /// <param name="workContext">工作上下文</param>
        /// <param name="serviceKey">控制器的服务密钥。</param>
        /// <param name="instance">控制器实例。</param>
        /// <returns>如果控制器已解析，则为真;否则错误。</returns>
        protected bool TryResolve<T>(WorkContext workContext, object serviceKey, out T instance) {
            if (workContext != null && serviceKey != null) {
                var key = new KeyedService(serviceKey, typeof (T));
                object value;
                if (workContext.Resolve<ILifetimeScope>().TryResolveService(key, out value)) {
                    instance = (T) value;
                    return true;
                }
            }

            instance = default(T);
            return false;
        }

        /// <summary>
        /// 根据控制器和区域的名称返回控制器类型。
        /// </summary>
        /// <param name="requestContext">从何处获取包含该区域的路由数据的请求上下文。</param>
        /// <param name="controllerName">控制器名称。</param>
        /// <returns>控制器类型。</returns>
        /// <example>控制器名称:Item, 区域:容器将返回 ItemController 类的类型。</example>
        protected override Type GetControllerType(RequestContext requestContext, string controllerName) {
            var routeData = requestContext.RouteData;

            // 确定请求的区域名称，然后返回到HotplugWeb控制器。
            var areaName = routeData.GetAreaName();

            // 服务名称模式与标识策略匹配
            var serviceKey = (areaName + "/" + controllerName).ToLowerInvariant();

            // 既然已知道请求容器, 请尝试解析控制器信息
            Meta<Lazy<IController>> info;
            var workContext = requestContext.GetWorkContext();
            if (TryResolve(workContext, serviceKey, out info)) {
                return (Type) info.Metadata["ControllerType"];
            }

            return null;
        }

        /// <summary>
        /// 返回控制器的实例。
        /// </summary>
        /// <param name="requestContext">从何处获取包含该区域的路由数据的请求上下文。</param>
        /// <param name="controllerType">控制器类型。</param>
        /// <returns>如果该控制器的类型已注册,则返回该控制器,否则返回null</returns>
        protected override IController GetControllerInstance(RequestContext requestContext, Type controllerType) {
            IController controller;
            var workContext = requestContext.GetWorkContext();
            if (TryResolve(workContext, controllerType, out controller)) {
                return controller;
            }

            //不适合MVC的预期。
            return base.GetControllerInstance(requestContext, controllerType);
        }
    }
}