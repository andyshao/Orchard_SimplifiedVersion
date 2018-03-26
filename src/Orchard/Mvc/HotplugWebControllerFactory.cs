using System;
using System.Web.Mvc;
using System.Web.Routing;
using Autofac;
using Autofac.Core;
using Autofac.Features.Metadata;
using HotplugWeb.Mvc.Extensions;

namespace HotplugWeb.Mvc {
    /// <summary>
    /// ��дĬ�Ͽ�����������ʹ��LoC�������������������ǵ���������ơ�
    /// </summary>
    public class HotplugWebControllerFactory : DefaultControllerFactory {
        /// <summary>
        /// ���Խ����빤��������������ĸ���������Կ�����Ŀ�������ʵ����
        /// </summary>
        /// <typeparam name="T">�����������͡�</typeparam>
        /// <param name="workContext">����������</param>
        /// <param name="serviceKey">�������ķ�����Կ��</param>
        /// <param name="instance">������ʵ����</param>
        /// <returns>����������ѽ�������Ϊ��;�������</returns>
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
        /// ���ݿ���������������Ʒ��ؿ��������͡�
        /// </summary>
        /// <param name="requestContext">�Ӻδ���ȡ�����������·�����ݵ����������ġ�</param>
        /// <param name="controllerName">���������ơ�</param>
        /// <returns>���������͡�</returns>
        /// <example>����������:Item, ����:���������� ItemController ������͡�</example>
        protected override Type GetControllerType(RequestContext requestContext, string controllerName) {
            var routeData = requestContext.RouteData;

            // ȷ��������������ƣ�Ȼ�󷵻ص�HotplugWeb��������
            var areaName = routeData.GetAreaName();

            // ��������ģʽ���ʶ����ƥ��
            var serviceKey = (areaName + "/" + controllerName).ToLowerInvariant();

            // ��Ȼ��֪����������, �볢�Խ�����������Ϣ
            Meta<Lazy<IController>> info;
            var workContext = requestContext.GetWorkContext();
            if (TryResolve(workContext, serviceKey, out info)) {
                return (Type) info.Metadata["ControllerType"];
            }

            return null;
        }

        /// <summary>
        /// ���ؿ�������ʵ����
        /// </summary>
        /// <param name="requestContext">�Ӻδ���ȡ�����������·�����ݵ����������ġ�</param>
        /// <param name="controllerType">���������͡�</param>
        /// <returns>����ÿ�������������ע��,�򷵻ظÿ�����,���򷵻�null</returns>
        protected override IController GetControllerInstance(RequestContext requestContext, Type controllerType) {
            IController controller;
            var workContext = requestContext.GetWorkContext();
            if (TryResolve(workContext, controllerType, out controller)) {
                return controller;
            }

            //���ʺ�MVC��Ԥ�ڡ�
            return base.GetControllerInstance(requestContext, controllerType);
        }
    }
}