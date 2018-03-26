using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Dispatcher;
using Autofac;
using Autofac.Core;
using Autofac.Features.Metadata;
using HotplugWeb.WebApi.Extensions;
using System.Diagnostics.Contracts;
using System.Web.Http.Tracing;
using System.Collections.Concurrent;
using System.Linq;

namespace HotplugWeb.WebApi {
    public class DefaultHotplugWebWebApiHttpControllerSelector :DefaultHttpControllerSelector,IHttpControllerSelector {
        private readonly HttpConfiguration _configuration;
        private readonly Lazy<ConcurrentDictionary<string, HttpControllerDescriptor>> _controllerInfoCache;
        public DefaultHotplugWebWebApiHttpControllerSelector(HttpConfiguration configuration) : base(configuration) {
            _controllerInfoCache = new Lazy<ConcurrentDictionary<string, HttpControllerDescriptor>>(InitializeControllerInfoCache);
            _configuration = configuration;
        }

        /// <summary>
        /// 尝试解析与工作上下文作用域的给定服务密钥关联的控制器的实例。
        /// </summary>
        /// <typeparam name="T">控制器的类型。</typeparam>
        /// <param name="workContext">工作上下文。</param>
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

        public override HttpControllerDescriptor SelectController(HttpRequestMessage request)
        {
            var routeData = request.GetRouteData();

            // 确定请求的区域名称, 然后退回到stock HotplugWeb 控制器
            var areaName = routeData.GetAreaName();

            var controllerName = base.GetControllerName(request);

            // 服务名称模式与标识策略匹配
            var serviceKey = (areaName + "/" + controllerName).ToLowerInvariant();

            var controllerContext = new HttpControllerContext(_configuration, routeData, request);

            // 既然已知道请求容器, 请尝试解析控制器信息
            Meta<Lazy<IHttpController>> info;
            var workContext = controllerContext.GetWorkContext();
            if (TryResolve(workContext, serviceKey, out info))
            {
                var type = (Type)info.Metadata["ControllerType"];

                return
                    new HttpControllerDescriptor(_configuration, controllerName, type);
            }

            return null;
        }
        public override IDictionary<string, HttpControllerDescriptor> GetControllerMapping()
        {
            return _controllerInfoCache.Value.ToDictionary(c => c.Key, c => c.Value, StringComparer.OrdinalIgnoreCase);
        }
        private ConcurrentDictionary<string, HttpControllerDescriptor> InitializeControllerInfoCache()
        {
            var result = new ConcurrentDictionary<string, HttpControllerDescriptor>(StringComparer.OrdinalIgnoreCase);
            var duplicateControllers = new HashSet<string>();
            IAssembliesResolver assembliesResolver = _configuration.Services.GetAssembliesResolver();
            IHttpControllerTypeResolver controllersResolver = _configuration.Services.GetHttpControllerTypeResolver();

            ICollection<Type> controllerTypes = controllersResolver.GetControllerTypes(assembliesResolver);
            var groupedByName = controllerTypes.GroupBy(
                t => t.Name.Substring(0, t.Name.Length - DefaultHttpControllerSelector.ControllerSuffix.Length),
                StringComparer.OrdinalIgnoreCase);

            var _controllerTypeCacheCache = groupedByName.ToDictionary(
                g => g.Key,
                g => g.ToLookup(t => t.Namespace ?? String.Empty, StringComparer.OrdinalIgnoreCase),
                StringComparer.OrdinalIgnoreCase);
            Dictionary<string, ILookup<string, Type>> controllerTypeGroups = _controllerTypeCacheCache;

            foreach (KeyValuePair<string, ILookup<string, Type>> controllerTypeGroup in controllerTypeGroups)
            {
                string controllerName = controllerTypeGroup.Key;

                foreach (IGrouping<string, Type> controllerTypesGroupedByNs in controllerTypeGroup.Value)
                {
                    foreach (Type controllerType in controllerTypesGroupedByNs)
                    {
                        if (result.Keys.Contains(controllerName))
                        {
                            duplicateControllers.Add(controllerName);
                            break;
                        }
                        else
                        {
                            result.TryAdd(controllerName, new HttpControllerDescriptor(_configuration, controllerName, controllerType));
                        }
                    }
                }
            }

            foreach (string duplicateController in duplicateControllers)
            {
                HttpControllerDescriptor descriptor;
                result.TryRemove(duplicateController, out descriptor);
            }

            return result;
        }
    }
}