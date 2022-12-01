using System.Threading;

namespace ContKor
{
    public interface IStack<T>
    {
        void Push(T item);
        bool TryPop(out T item);
        int Count { get; }
    }
    
    public class MyStack<T> : IStack<T>
    {
        private Node _head;

        public int Count 
        { 
            get
            {
                if (_head == null) return 0;
                return _head.Quantity; 
            } 
        }

        public void Push(T item)
        {
            var spin = new SpinWait();
            while (true)
            {
                var previousHead = _head;
                var quantity = 1;
                if (previousHead != null) quantity += previousHead.Quantity;
                var node = new Node(item, previousHead, quantity);
                if (Interlocked.CompareExchange(ref _head, node, previousHead) == previousHead) return;
                spin.SpinOnce();
            }
        }

        public bool TryPop(out T item)
        {
            item = default;
            var spin = new SpinWait();
            while (true)
            {
                if (_head == null) return false;
                var oldHead = _head;
                if (Interlocked.CompareExchange(ref _head, oldHead.Next, oldHead) == oldHead)
                {
                    item = oldHead.Value;
                    return true;
                }
                spin.SpinOnce();
            }
        }

        private class Node
        {
            public readonly T Value;
            public readonly Node Next;
            public readonly int Quantity;
            public Node(T value, Node next, int quantity)
            {
                Value = value;
                Next = next;
                Quantity = quantity;
            }
        }
    }
}
