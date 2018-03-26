using System;
using System.Collections.Generic;
using System.Linq;

namespace HotplugWeb.Commands {
    [AttributeUsage(AttributeTargets.Method)]
    public class HotplugWebSwitchesAttribute : Attribute {
        private readonly string _switches;

        public HotplugWebSwitchesAttribute(string switches) {
            _switches = switches;
        }

        public IEnumerable<string> Switches {
            get {
                return (_switches ?? "").Trim().Split(',').Select(s => s.Trim());
            }
        }
    }
}
