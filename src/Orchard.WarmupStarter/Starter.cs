using System;
using System.Threading;
using System.Web;

namespace Orchard.WarmupStarter
{
    public class Starter<T> where T : class
    {
        private readonly Func<HttpApplication, T> _initialization;
        private readonly Action<HttpApplication, T> _beginRequest;
        private readonly Action<HttpApplication, T> _endRequest;
        private readonly object _synLock = new object();
        /// <summary>
        /// 初始化队列工作项的结果。
        /// 仅在初始化完成时才设置。
        /// </summary>
        private volatile T _initializationResult;
        /// <summary>
        /// 初始化线程引发的(可能的)错误。
        /// 这是一个“一次性”错误信号，这样我们就可以在另一个请求进来时重新启动初始化。
        /// </summary>
        private volatile Exception _error;
        /// <summary>
        /// 前一个初始化的(潜在)错误。
        /// 我们需要将此错误保持为活动状态，直到完成下一个初始化，
        /// 这样我们才能继续为所有传入的请求报告错误。
        /// </summary>
        private volatile Exception _previousError;

        public Starter(Func<HttpApplication, T> initialization, Action<HttpApplication, T> beginRequest, Action<HttpApplication, T> endRequest)
        {
            _initialization = initialization;
            _beginRequest = beginRequest;
            _endRequest = endRequest;
        }

        public void OnApplicationStart(HttpApplication application)
        {
            LaunchStartupThread(application);
        }

        public void OnBeginRequest(HttpApplication application)
        {
            // 初始化导致错误。
            if (_error != null)
            {
                // 为下一个请求保存错误并重新启动异步初始化。
                // 请注意: 我们必须重试初始化的原因是，
                // 应用程序环境可能会在请求之间发生变化，
                // 例如，App_Data是为AppPool进行读写的。
                bool restartInitialization = false;

                lock (_synLock)
                {
                    if (_error != null)
                    {
                        _previousError = _error;
                        _error = null;
                        restartInitialization = true;
                    }
                }

                if (restartInitialization)
                {
                    LaunchStartupThread(application);
                }
            }

            // 先前的初始化导致了一个错误(另一个初始化正在运行)
            if (_previousError != null)
            {
                throw new ApplicationException("应用程序初始化期间出错", _previousError);
            }

            // 只有在初始化成功完成时才通知。
            if (_initializationResult != null)
            {
                _beginRequest(application, _initializationResult);
            }
        }

        public void OnEndRequest(HttpApplication application)
        {
            // 只有在初始化成功完成时才通知。
            if (_initializationResult != null)
            {
                _endRequest(application, _initializationResult);
            }
        }

        /// <summary>
        /// 在队列工作项中异步运行初始化委托。
        /// </summary>
        public void LaunchStartupThread(HttpApplication application)
        {
            // 确保传入的请求已排队。
            WarmupHttpModule.SignalWarmupStart();

            ThreadPool.QueueUserWorkItem(
                state => {
                    try
                    {
                        var result = _initialization(application);
                        _initializationResult = result;
                    }
                    catch (Exception ex)
                    {
                        lock (_synLock)
                        {
                            _error = ex;
                            _previousError = null;
                        }
                    }
                    finally
                    {
                        // 在初始化结束时执行挂起的请求。
                        WarmupHttpModule.SignalWarmupDone();
                    }
                });
        }
    }

}
