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
        /// ���Խ����빤��������������ĸ���������Կ�����Ŀ�������ʵ����
        /// </summary>
        /// <typeparam name="T">�����������͡�</typeparam>
        /// <param name="workContext">����������</param>
        /// <param name="serviceKey">�������ķ�����Կ��</param>
        /// <param name="instance">������ʵ����</param>
        /// <returns>����������ѽ���, ��Ϊ True; ����Ϊfalse��</returns>
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

            // ȷ���������������, Ȼ���˻ص�stock HotplugWeb ������
            var areaName = routeData.GetAreaName();

            //��������ģʽ���ʶ����ƥ��
            var serviceKey = (areaName + "/" + controllerDescriptor.ControllerName).ToLowerInvariant();

            // ��Ȼ��֪����������, �볢�Խ�����������Ϣ
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