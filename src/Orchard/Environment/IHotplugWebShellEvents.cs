using HotplugWeb.Events;

namespace HotplugWeb.Environment {
    public interface IHotplugWebShellEvents : IEventHandler {
        void Activated();
        void Terminating();
    }
}
