using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ContKor
{
    public interface IMultiLock
    {
        public IDisposable AcquireLock(params string[] keys);
    }

    public class Disposable : IDisposable
    {
        public readonly Action _onDispose;
        public Disposable(Action onDispose) => _onDispose = onDispose;
        public void Dispose() => _onDispose();
    }
    
    public class MultiLock : IMultiLock
    {
        private readonly Dictionary<string, object> lockObjs;

        public MultiLock(params string[] keys)
        {
            lockObjs = new Dictionary<string, object>();
            foreach (var key in keys)
            {
                lockObjs.Add(key, new object());
            }
        }

        public IDisposable AcquireLock(params string[] keys)
        {
            Array.Sort(keys);
            var blocks = new Stack<object>(); // объекты, на которые есть блокировка
            try
            {
                // для каждого ключа берем его объект блокировки
                var lockObjects = keys.Select(k => lockObjs[k]);
                foreach (var lockObj in lockObjects)
                {
                    // захватываем на него блокировку
                    var lockTaken = false;
                    try
                    {
                        Monitor.TryEnter(lockObj, ref lockTaken);
                    }
                    finally
                    {
                        if(lockTaken)
                            blocks.Push(lockObj);
                    }
                }
            }
            catch (Exception)
            {
                // вылетело исключение -- освобождаем те блокировки, которые успели захватить до
                Unlock(blocks);
                // перевыброс исключения
                throw;
            }

            return new Disposable(() => Unlock(blocks));
        }

        private void Unlock(Stack<object> blocks)
        {
            while (true)
            {
                if(blocks.Count == 0) break;
                var block = blocks.Pop();
                Monitor.Exit(block);
            }
        }
    }

    public static class Program
    {
        public static void Main()
        {
            var multiLock = new MultiLock("l", "m", "a", "o");
            using (multiLock.AcquireLock("a", "l"))
            {
                Console.WriteLine(".");
            }
        }
    }
}