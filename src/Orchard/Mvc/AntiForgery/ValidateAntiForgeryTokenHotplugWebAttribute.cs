using System;
using System.Web.Mvc;

namespace HotplugWeb.Mvc.AntiForgery {
    [AttributeUsage(AttributeTargets.Method)]
    public class ValidateAntiForgeryTokenHotplugWebAttribute : FilterAttribute {
        private readonly bool _enabled = true;

        public ValidateAntiForgeryTokenHotplugWebAttribute() : this(true) {}

        public ValidateAntiForgeryTokenHotplugWebAttribute(bool enabled) {
            _enabled = enabled;
        }

        public bool Enabled { get { return _enabled; } }
    }
}