using System;

namespace HotplugWeb.Environment.Extensions {
    [AttributeUsage(AttributeTargets.Class)]
    public class HotplugWebFeatureAttribute : Attribute {
        public HotplugWebFeatureAttribute(string text) {
            FeatureName = text;
        }

        public string FeatureName { get; set; }
    }
}