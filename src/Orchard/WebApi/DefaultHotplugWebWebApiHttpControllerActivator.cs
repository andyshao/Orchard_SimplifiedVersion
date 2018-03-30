using System;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Dispatcher;
using Autofac;
using Autofac.Core;
using Autofac.Features.Metadata;
using HotplugWeb.WebApi.Extensions;

namespace HotplugWeb.WebApi {
    public class DefaultHotplugWebWebApiHttpControllerActivator : IHttpControllerActivator {
        private readonly HttpConfiguration _configuration;

        public DefaultHotplugWebWebApiHttpControllerActivator(HttpConfiguration configuration)
            : base() {
            _configuration = configuration;
        }

        /// <summary>
        /// 尝试解析与工作上下文作用域的给定服务密钥关联的控制器的实例。
        /// </summary>
        /// <typeparam name="T">控制器的类型。</typeparam>
        /// <param name="workContext">工作上下文</param>
        /// <param name="serviceKey">控制器的服务密钥。</param>
        /// <param name="instance">控制器实例。</param>
        /// <returns>如果控制器已解析, 则为 True; 否则为false。</returns>
        protected bool TryResolve<T>(WorkContext workContext, object serviceKey, out T instance) {
            if (workContext != null && serviceKey != null) {
                var key = new KeyedService(serviceKey, typeof(T));
                object value;
                if (workContext.Resolve<ILifetimeScope>().TryResolveService(key, out value)) {
                    instance = (T)value;
                    return true;
                }
            }

            instance = default(T);
            return false;
        }
        public IHttpController Create(HttpRequestMessage request, HttpControllerDescriptor controllerDescriptor, Type controllerType) {
            var routeData = request.GetRouteData();

            HttpControllerContext controllerContext = new HttpControllerContext(_configuration, routeData, request);

            // 确定请求的区域名称, 然后退回到stock HotplugWeb 控制器
            var areaName = routeData.GetAreaName();

            //服务名称模式与标识策略匹配
            var serviceKey = (areaName + "/" + controllerDescriptor.ControllerName).ToLowerInvariant();

            // 既然已知道请求容器, 请尝试解析控制器信息
            Meta<Lazy<IHttpController>> info;
            var workContext = controllerContext.GetWorkContext();
            if (TryResolve(workContext, serviceKey, out info)) {
                controllerContext.ControllerDescriptor =
                    new HttpControllerDescriptor(_configuration, controllerDescriptor.ControllerName, controllerType);

                var controller = info.Value.Value;

                controllerContext.Controller = controller;

                return controller;
            }

            return null;
        }
    }
}