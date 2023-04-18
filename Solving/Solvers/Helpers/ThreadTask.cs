using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FFXIVCraftingLib.Solving.Solvers.Helpers
{
    public class ThreadAction
    {
        private Thread Thread;
        private Action Method;

        public bool IsRunning { get; private set; }
        public bool Completed { get; private set; }

        public ThreadAction(Action method, bool start = true)
        {
            if (method == null)
                throw new ArgumentNullException("method is null");

            Method = method;
            IsRunning = false;
            Completed = false;

            if (start)
                Start();
        }

        public void Start()
        {
            if (IsRunning) return;
            IsRunning = true;
            Thread = new Thread(Loop);
        }

        private void Loop()
        {
            Method();
            IsRunning = false;
        }
    }

    public class ThreadAction<T>
    {
        private Thread Thread;
        private Action<T> Method;

        public bool IsRunning { get; private set; }
        public bool Completed { get; private set; }

        protected T Param1;

        public ThreadAction(Action<T> method, bool start, T param1)
        {
            if (method == null)
                throw new ArgumentNullException("method is null");

            Method = method;
            IsRunning = false;
            Completed = false;

            if (start)
                Start();
        }

        public void Start()
        {
            if (IsRunning) return;
            IsRunning = true;
            Thread = new Thread(Loop);
        }

        protected virtual void Loop()
        {
            Method(Param1);
            IsRunning = false;
        }
    }

    public class ThreadTask<TResult>
    {
        private Thread Thread;
        private Func<TResult> Method;

        public bool IsRunning { get; private set; }
        public bool Completed { get; private set; }
        private TResult Value;

        public ThreadTask(Func<TResult> method, bool start = true)
        {
            if (method == null)
                throw new ArgumentNullException("method is null");

            Method = method;
            IsRunning = false;
            Completed = false;

            if (start)
                Start();
        }

        public void Start()
        {
            if (IsRunning) return;
            IsRunning = true;
            Thread = new Thread(Loop);
        }

        private void Loop()
        {
            Value = Method();
            IsRunning = false;
        }

        public TResult Result
        {
            get
            {
                while (IsRunning) Thread.Sleep(10);
                return Value;
            }
        }
    }
}
