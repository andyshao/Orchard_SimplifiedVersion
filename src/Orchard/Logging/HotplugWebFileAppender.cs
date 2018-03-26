using System.Collections.Generic;
using log4net.Appender;
using log4net.Util;

namespace HotplugWeb.Logging {
    public class HotplugWebFileAppender : RollingFileAppender {
        /// <summary>
        /// 已经知道的后缀的字典(基于先前的尝试)为给定的文件名。
        /// </summary>
        private static readonly Dictionary<string, int> _suffixes = new Dictionary<string, int>();

        /// <summary>
        /// 将在每个OpenFile方法调用上进行的后缀尝试的数量。
        /// </summary>
        private const int Retries = 50;

        /// <summary>
        /// 在清除之前记录的最大的后缀数量，以回收内存。
        /// </summary>
        private const int MaxSuffixes = 100;

        /// <summary>
        /// 打开日志文件，如果由于openning失败(通常是锁定)，则向文件名添加一个增量后缀。
        /// </summary>
        /// <param name="fileName">在配置文件中指定的文件名。.</param>
        /// <param name="append">指示标志如果日志文件已经存在, 则应追加它。</param>
        protected override void OpenFile(string fileName, bool append) {
            lock (this) {
                bool fileOpened = false;
                string completeFilename = GetNextOutputFileName(fileName);
                string currentFilename = fileName;

                if (_suffixes.Count > MaxSuffixes) {
                    _suffixes.Clear();
                }

                if (!_suffixes.ContainsKey(completeFilename)) {
                    _suffixes[completeFilename] = 0;
                }

                int newSuffix = _suffixes[completeFilename];

                for (int i = 1; !fileOpened && i <= Retries; i++) {
                    try {
                        if (newSuffix > 0) {
                            currentFilename = string.Format("{0}-{1}", fileName, newSuffix);
                        }

                        BaseOpenFile(currentFilename, append);

                        fileOpened = true;
                    } catch {
                        newSuffix = _suffixes[completeFilename] + i;

                        LogLog.Error(typeof(HotplugWebFileAppender), string.Format("HotplugWebFileAppender: Failed to open [{0}]. Attempting [{1}-{2}] instead.", fileName, fileName, newSuffix));
                    }
                }

                _suffixes[completeFilename] = newSuffix;
            }
        }

        /// <summary>
        /// 调用基类 OpenFile 方法。
        /// </summary>
        /// <param name="fileName">配置文件中指定的文件名。</param>
        /// <param name="append">指示标志如果日志文件已经存在, 则应追加它。</param>
        protected virtual void BaseOpenFile(string fileName, bool append) {
            base.OpenFile(fileName, append);
        }
    }
}
