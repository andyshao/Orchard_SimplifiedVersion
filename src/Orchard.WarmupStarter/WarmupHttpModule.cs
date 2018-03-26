using System;
using System.Collections.Generic;
using System.Threading;
using System.Web;

namespace Orchard.WarmupStarter
{
    public class WarmupHttpModule : IHttpModule
    {
        private HttpApplication _context;
        private static object _synLock = new object();
        private static IList<Action> _awaiting = new List<Action>();

        public void Init(HttpApplication context)
        {
            _context = context;
            context.AddOnBeginRequestAsync(BeginBeginRequest, EndBeginRequest, null);
        }

        public void Dispose()
        {
        }

        private static bool InWarmup()
        {
            lock (_synLock)
            {
                return _awaiting != null;
            }
        }

        /// <summary>
        /// 预热代码即将开始:任何新的传入请求都将排队，直到调用“SignalWarmupDone”。
        /// </summary>
        public static void SignalWarmupStart()
        {
            lock (_synLock)
            {
                if (_awaiting == null)
                {
                    _awaiting = new List<Action>();
                }
            }
        }

        /// <summary>
        /// 刚刚完成的预热代码:处理“_wait”队列中的所有挂起请求，现在立即处理任何新的传入请求。
        /// </summary>
        public static void SignalWarmupDone()
        {
            IList<Action> temp;

            lock (_synLock)
            {
                temp = _awaiting;
                _awaiting = null;
            }

            if (temp != null)
            {
                foreach (var action in temp)
                {
                    action();
                }
            }
        }

        /// <summary>
        /// 根据当前模式排队或直接处理动作。.
        /// </summary>
        private void Await(Action action)
        {
            Action temp = action;

            lock (_synLock)
            {
                if (_awaiting != null)
                {
                    temp = null;
                    _awaiting.Add(action);
                }
            }

            if (temp != null)
            {
                temp();
            }
        }

        private IAsyncResult BeginBeginRequest(object sender, EventArgs e, AsyncCallback cb, object extradata)
        {
            // 主机可用，处理每个请求，或处理文件。
            if (!InWarmup() || WarmupUtility.DoBeginRequest(_context))
            {
                var asyncResult = new DoneAsyncResult(extradata);
                cb(asyncResult);
                return asyncResult;
            }
            else
            {
                // 这是“on hold”执行路径。
                var asyncResult = new WarmupAsyncResult(cb, extradata);
                Await(asyncResult.Completed);
                return asyncResult;
            }
        }

        private static void EndBeginRequest(IAsyncResult ar)
        {
        }

        /// <summary>
        /// “on hold”请求的AsyncResult(在“Completed()”被调用时恢复)
        /// </summary>
        private class WarmupAsyncResult : IAsyncResult
        {
            private readonly EventWaitHandle _eventWaitHandle = new AutoResetEvent(false/*initialState*/);
            private readonly AsyncCallback _cb;
            private readonly object _asyncState;
            private bool _isCompleted;

            public WarmupAsyncResult(AsyncCallback cb, object asyncState)
            {
                _cb = cb;
                _asyncState = asyncState;
                _isCompleted = false;
            }

            public void Completed()
            {
                _isCompleted = true;
                _eventWaitHandle.Set();
                _cb(this);
            }

            bool IAsyncResult.CompletedSynchronously
            {
                get { return false; }
            }

            bool IAsyncResult.IsCompleted
            {
                get { return _isCompleted; }
            }

            object IAsyncResult.AsyncState
            {
                get { return _asyncState; }
            }

            WaitHandle IAsyncResult.AsyncWaitHandle
            {
                get { return _eventWaitHandle; }
            }
        }

        /// <summary>
        /// 异步结果“现在可以处理”请求。
        /// </summary>
        private class DoneAsyncResult : IAsyncResult
        {
            private readonly object _asyncState;
            private static readonly WaitHandle _waitHandle = new ManualResetEvent(true/*initialState*/);

            public DoneAsyncResult(object asyncState)
            {
                _asyncState = asyncState;
            }

            bool IAsyncResult.CompletedSynchronously
            {
                get { return true; }
            }

            bool IAsyncResult.IsCompleted
            {
                get { return true; }
            }

            WaitHandle IAsyncResult.AsyncWaitHandle
            {
                get { return _waitHandle; }
            }

            object IAsyncResult.AsyncState
            {
                get { return _asyncState; }
            }
        }
    }
}