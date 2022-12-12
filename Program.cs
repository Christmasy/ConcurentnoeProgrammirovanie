using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace CustomThreadPool
{
    public static class Program
    {
        public static void Main()
        {
            //ThreadPoolTests.Run<DotNetThreadPoolWrapper>();
            ThreadPoolTests.Run<ThreadPool>();
        }
    }

    public class ThreadPool : IThreadPool, IDisposable
    {
        private readonly Worker[] _workers;
        private readonly Queue<Action> _globalQueue;
        private long _finishedTask;
        private volatile int _quantityOfWaitThreads;
        public ThreadPool() : this(Environment.ProcessorCount) { }

        private ThreadPool(int threadCount)
        {
            _globalQueue = new Queue<Action>();
            if (threadCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(threadCount));
            _workers = Enumerable.Range(0, threadCount).Select(_ => 
            {
                var worker = new Worker(DispatchLoop);
                worker.Thread.Start();
                return worker;
            }
            ).ToArray();
        }
        
        public void EnqueueAction(Action action)
        {
            if (Worker.CurrentWorker.Value != null) LocalQueuePushTask(action);
            else
                lock (_globalQueue)
                    _globalQueue.Enqueue(action);
            if (_quantityOfWaitThreads <= 0) return;

            lock (_globalQueue)
                Monitor.Pulse(_globalQueue);
        }

        public long GetTasksProcessedCount() => _finishedTask;

        private static void LocalQueuePushTask(Action action)
        {
            if(Worker.CurrentWorker.Value != null)
                Worker.CurrentWorker.Value!.LocalQueue.LocalPush(action);
            else throw new InvalidOperationException("This thread is not a worker");
        }

        private void DispatchLoop(Worker worker)
        {
            while (true)
            {
                GetTask().Invoke();
                Interlocked.Increment(ref _finishedTask);
            }

            Action GetTask()
            {
                if (GetTaskFromLocalQueue(worker, out var task))
                    return task!;
                while (true)
                {
                    bool canGetFromGlobal;
                    lock (_globalQueue)
                        canGetFromGlobal = _globalQueue.TryDequeue(out task);
                    if (canGetFromGlobal || StealTask(worker, out task))
                        return task!;
                    lock (_globalQueue)
                    {
                        _quantityOfWaitThreads++;
                        try
                        {
                            Monitor.Wait(_globalQueue);
                        }
                        finally
                        {
                            _quantityOfWaitThreads--;
                        }
                    }
                }
            }
        }

        bool GetTaskFromLocalQueue(Worker worker, out Action? task)
        {
            task = null;
            return worker.LocalQueue.LocalPop(ref task!);
        }

        bool StealTask(Worker worker, out Action? task)
        {
            task = null;
            var workersExceptWorker = _workers?.Where(w => w != worker) ?? Enumerable.Empty<Worker>();
            foreach (var w in workersExceptWorker)
                if (w.LocalQueue.TrySteal(ref task!))
                    return true;
            return false;
        }

        public void Dispose() => _workers.ToList().ForEach(w => w.Dispose());

        private class Worker : IDisposable
        {
            public static readonly ThreadLocal<Worker> CurrentWorker = new ThreadLocal<Worker>();
            public WorkStealingQueue<Action> LocalQueue { get; } = new WorkStealingQueue<Action>();
            public Thread Thread { get; }
            public Worker(Action<Worker> dispatchLoop)
            {
                Thread = new Thread(ThreadFunc)
                {
                    IsBackground = true
                };

                void ThreadFunc()
                {
                    CurrentWorker.Value = this;
                    dispatchLoop(this);
                }
            }
            public void Dispose() => CurrentWorker.Dispose();
        }
    }
}