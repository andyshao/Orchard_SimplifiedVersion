using System;
using System.Runtime.Serialization;
using HotplugWeb.ContentManagement;
using HotplugWeb.Localization;

namespace HotplugWeb.Security {
    [Serializable]
    public class HotplugWebSecurityException : HotplugWebCoreException {
        public HotplugWebSecurityException(LocalizedString message) : base(message) { }
        public HotplugWebSecurityException(LocalizedString message, Exception innerException) : base(message, innerException) { }
        protected HotplugWebSecurityException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        public string PermissionName { get; set; }
        public IUser User { get; set; }
        public IContent Content { get; set; }
    }
}
