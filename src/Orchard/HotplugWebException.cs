using System;
using System.Runtime.Serialization;
using HotplugWeb.Localization;

namespace HotplugWeb {
    [Serializable]
    public class HotplugWebException : ApplicationException {
        private readonly LocalizedString _localizedMessage;

        public HotplugWebException(LocalizedString message)
            : base(message.Text) {
            _localizedMessage = message;
        }

        public HotplugWebException(LocalizedString message, Exception innerException)
            : base(message.Text, innerException) {
            _localizedMessage = message;
        }

        protected HotplugWebException(SerializationInfo info, StreamingContext context)
            : base(info, context) {
        }

        public LocalizedString LocalizedMessage { get { return _localizedMessage; } }
    }
}