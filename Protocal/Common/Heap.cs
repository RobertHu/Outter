using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Protocal.Common
{
    public sealed class Heap<T> where T : IComparable<T>
    {
        private T[] _elements = new T[5000];
        private int _heapSize;

        public Heap()
        {
            _heapSize = 0;
        }

        public int Count
        {
            get { return _heapSize; }
        }

        public T Pick()
        {
            if (_heapSize <= 0)
                throw new IndexOutOfRangeException("the head is empty");
            return _elements[0];
        }

        public T Pop()
        {
            if (_heapSize <= 0)
                throw new IndexOutOfRangeException("the head is empty");
            T result = _elements[0];
            _elements[0] = _elements[(_heapSize - 1)];
            _heapSize--;
            this.MinHeapify(0);
            return result;
        }


        public void Add(T x)
        {
            _heapSize++;
            _elements[_heapSize - 1] = x;
            int i = _heapSize - 1;
            while (i > 0 && x.CompareTo(_elements[Parent(i)]) < 0)
            {
                T temp = _elements[Parent(i)];
                _elements[Parent(i)] = _elements[i];
                _elements[i] = temp;
                i = Parent(i);
            }
        }


        private void BuildHeap()
        {
            for (int i = (_heapSize - 1) / 2; i >= 0; i--)
            {
                this.MinHeapify(i);
            }
        }


        private void MinHeapify(int i)
        {
            int left = Left(i);
            int right = Right(i);
            int smallest;
            if (left < _heapSize && _elements[left].CompareTo(_elements[i]) < 0)
            {
                smallest = left;
            }
            else
            {
                smallest = i;
            }

            if (right < _heapSize && _elements[right].CompareTo(_elements[smallest]) < 0)
            {
                smallest = right;
            }

            if (smallest != i)
            {
                T temp = _elements[smallest];
                _elements[smallest] = _elements[i];
                _elements[i] = temp;
                this.MinHeapify(smallest);
            }
        }


        private int Left(int i)
        {
            return 2 * i + 1;
        }

        private int Right(int i)
        {
            return 2 * i + 2;
        }

        private int Parent(int i)
        {
            return (int)Math.Floor((i - 1) / 2.0);
        }
    }
}
