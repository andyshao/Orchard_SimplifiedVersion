using System.Collections.Generic;
using System.Reflection;

namespace HotplugWeb.Environment {
    public interface IHotplugWebFrameworkAssemblies : IDependency {
        IEnumerable<AssemblyName> GetFrameworkAssemblies();
    }

    public class DefaultHotplugWebFrameworkAssemblies : IHotplugWebFrameworkAssemblies {
        public IEnumerable<AssemblyName> GetFrameworkAssemblies() {
            return typeof (IDependency).Assembly.GetReferencedAssemblies();
        }
    }
}
