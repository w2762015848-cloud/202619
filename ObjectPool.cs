using System.Collections.Generic;

namespace BattleSystem
{
    public class ObjectPool<T> where T : class
    {
        private readonly Stack<T> _stack;
        private readonly System.Func<T> _createFunc;
        private readonly System.Action<T> _actionOnGet;
        private readonly System.Action<T> _actionOnRelease;
        private readonly System.Action<T> _actionOnDestroy;
        private readonly int _maxSize;
        private int _activeCount;

        public int CountAll => _activeCount + _stack.Count;
        public int CountActive => _activeCount;
        public int CountInactive => _stack.Count;

        public ObjectPool(
            System.Func<T> createFunc,
            System.Action<T> actionOnGet = null,
            System.Action<T> actionOnRelease = null,
            System.Action<T> actionOnDestroy = null,
            int defaultCapacity = 10,
            int maxSize = 100)
        {
            if (createFunc == null)
                throw new System.ArgumentNullException(nameof(createFunc));

            _stack = new Stack<T>(defaultCapacity);
            _createFunc = createFunc;
            _actionOnGet = actionOnGet;
            _actionOnRelease = actionOnRelease;
            _actionOnDestroy = actionOnDestroy;
            _maxSize = maxSize;
        }

        public T Get()
        {
            T element;
            if (_stack.Count == 0)
            {
                element = _createFunc();
                _activeCount++;
            }
            else
            {
                element = _stack.Pop();
                _activeCount++;
            }

            _actionOnGet?.Invoke(element);
            return element;
        }

        public void Release(T element)
        {
            if (_stack.Count > 0 && ReferenceEquals(_stack.Peek(), element))
            {
                throw new System.InvalidOperationException("Internal error. Trying to destroy object that is already released to pool.");
            }

            _actionOnRelease?.Invoke(element);

            if (_stack.Count < _maxSize)
            {
                _stack.Push(element);
            }
            else
            {
                _actionOnDestroy?.Invoke(element);
            }

            _activeCount--;
        }

        public void Clear()
        {
            foreach (var item in _stack)
            {
                _actionOnDestroy?.Invoke(item);
            }

            _stack.Clear();
            _activeCount = 0;
        }
    }
}