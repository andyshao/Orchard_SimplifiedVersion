using System;
using System.Runtime.Serialization;
using HotplugWeb.Localization;

namespace HotplugWeb {
    [Serializable]
    public class HotplugWebCoreException : Exception {
        private readonly LocalizedString _localizedMessage;

        public HotplugWebCoreException(LocalizedString message)
            : base(message.Text) {
            _localizedMessage = message;
        }

        public HotplugWebCoreException(LocalizedString message, Exception innerException)
            : base(message.Text, innerException) {
            _localizedMessage = message;
        }

        protected HotplugWebCoreException(SerializationInfo info, StreamingContext context)
            : base(info, context) {
        }

        public LocalizedString LocalizedMessage { get { return _localizedMessage; } }
    }
}