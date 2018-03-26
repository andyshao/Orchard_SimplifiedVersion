using System;

namespace HotplugWeb.Environment.Extensions {
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class HotplugWebSuppressDependencyAttribute : Attribute {
        public HotplugWebSuppressDependencyAttribute(string fullName) {
            FullName = fullName;
        }

        public string FullName { get; set; }
    }
}