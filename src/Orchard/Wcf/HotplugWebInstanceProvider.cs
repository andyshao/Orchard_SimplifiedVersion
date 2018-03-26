using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;
using Autofac;
using Autofac.Core;

namespace HotplugWeb.Wcf {
    public class HotplugWebInstanceProvider : IInstanceProvider {
        private readonly IWorkContextAccessor _workContextAccessor;
        private readonly IComponentRegistration _componentRegistration;

        public HotplugWebInstanceProvider(IWorkContextAccessor workContextAccessor, IComponentRegistration componentRegistration) {
            _workContextAccessor = workContextAccessor;
            _componentRegistration = componentRegistration;
        }

        public object GetInstance(InstanceContext instanceContext, Message message) {
            HotplugWebInstanceContext item = new HotplugWebInstanceContext(_workContextAccessor);
            instanceContext.Extensions.Add(item);
            return item.Resolve(_componentRegistration);

        }

        public object GetInstance(InstanceContext instanceContext) {
            return GetInstance(instanceContext, null);
        }

        public void ReleaseInstance(InstanceContext instanceContext, object instance) {
            HotplugWebInstanceContext context = instanceContext.Extensions.Find<HotplugWebInstanceContext>();
            if (context != null) {
                context.Dispose();
            }
        }
    }
}
