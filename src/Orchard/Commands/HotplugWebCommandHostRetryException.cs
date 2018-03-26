using System;
using System.Runtime.Serialization;
using HotplugWeb.Localization;

namespace HotplugWeb.Commands {
    [Serializable]
    public class HotplugWebCommandHostRetryException : HotplugWebCoreException {
        public HotplugWebCommandHostRetryException(LocalizedString message)
            : base(message) {
        }

        public HotplugWebCommandHostRetryException(LocalizedString message, Exception innerException)
            : base(message, innerException) {
        }

        protected HotplugWebCommandHostRetryException(SerializationInfo info, StreamingContext context)
            : base(info, context) {
        }
    }
}