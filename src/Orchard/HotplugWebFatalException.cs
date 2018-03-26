using System;
using System.Runtime.Serialization;
using HotplugWeb.Localization;

namespace HotplugWeb {
    [Serializable]
    public class HotplugWebFatalException : Exception {
        private readonly LocalizedString _localizedMessage;

        public HotplugWebFatalException(LocalizedString message)
            : base(message.Text) {
            _localizedMessage = message;
        }

        public HotplugWebFatalException(LocalizedString message, Exception innerException)
            : base(message.Text, innerException) {
            _localizedMessage = message;
        }

        protected HotplugWebFatalException(SerializationInfo info, StreamingContext context)
            : base(info, context) {
        }

        public LocalizedString LocalizedMessage { get { return _localizedMessage; } }
    }
}
