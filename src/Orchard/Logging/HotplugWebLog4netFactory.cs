using System;
using System.Configuration;
using Castle.Core.Logging;
using log4net;
using log4net.Config;
using HotplugWeb.Environment;

namespace HotplugWeb.Logging {
    public class HotplugWebLog4netFactory : AbstractLoggerFactory {
        private static bool _isFileWatched = false;

        public HotplugWebLog4netFactory(IHostEnvironment hostEnvironment) 
            : this(ConfigurationManager.AppSettings["log4net.Config"], hostEnvironment) { }

        public HotplugWebLog4netFactory(string configFilename, IHostEnvironment hostEnvironment) {
            if (!_isFileWatched && !string.IsNullOrWhiteSpace(configFilename)) {
                // 只在完全信任的情况下监视配置文件。
                XmlConfigurator.ConfigureAndWatch(GetConfigFile(configFilename));
                _isFileWatched = true;
            }
        }

        public override Castle.Core.Logging.ILogger Create(string name, LoggerLevel level) {
            throw new NotSupportedException("无法在运行时设置记录器级别。请检查配置文件。");
        }

        public override Castle.Core.Logging.ILogger Create(string name) {
            return new HotplugWebLog4netLogger(LogManager.GetLogger(name), this);
        }
    }
}
