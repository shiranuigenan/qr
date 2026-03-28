using System.Diagnostics;
using System.Text;

namespace QRCoder;

public partial class QRCodeGenerator
{
    private struct Polynom : IDisposable
    {
        private PolynomItem[] _polyItems;
        public Polynom(int count)
        {
            Count = 0;
            _polyItems = RentArray(count);
        }
        public void Add(PolynomItem item)
        {
            AssertCapacity(Count + 1);
            _polyItems[Count++] = item;
        }
        public void RemoveAt(int index)
        {
            if ((uint)index >= (uint)Count)
                ThrowIndexArgumentOutOfRangeException();

            if (index < Count - 1)
                Array.Copy(_polyItems, index + 1, _polyItems, index, Count - index - 1);

            Count--;
        }
        public PolynomItem this[int index]
        {
            get
            {
                if ((uint)index >= Count)
                    ThrowIndexArgumentOutOfRangeException();
                return _polyItems[index];
            }
            set
            {
                if ((uint)index >= Count)
                    ThrowIndexArgumentOutOfRangeException();
                _polyItems[index] = value;
            }
        }
        [StackTraceHidden]
        private static void ThrowIndexArgumentOutOfRangeException() => throw new ArgumentOutOfRangeException("index");
        public int Count { get; private set; }
        public void Clear() => Count = 0;
        public Polynom Clone()
        {
            var newPolynom = new Polynom(Count);
            Array.Copy(_polyItems, newPolynom._polyItems, Count);
            newPolynom.Count = Count;
            return newPolynom;
        }
        public void Sort(Func<PolynomItem, PolynomItem, int> comparer)
        {
            if (comparer == null)
                throw new ArgumentNullException(nameof(comparer));

            var items = _polyItems ?? throw new ObjectDisposedException(nameof(Polynom));

            if (Count <= 1)
            {
                return; // Nothing to sort if the list is empty or contains only one element
            }

            void QuickSort(int left, int right)
            {
                int i = left;
                int j = right;
                var pivot = items[(left + right) / 2];

                while (i <= j)
                {
                    while (comparer(items[i], pivot) < 0)
                        i++;
                    while (comparer(items[j], pivot) > 0)
                        j--;

                    if (i <= j)
                    {
                        // Swap items[i] and items[j]
                        var temp = items[i];
                        items[i] = items[j];
                        items[j] = temp;
                        i++;
                        j--;
                    }
                }

                // Recursively sort the sub-arrays
                if (left < j)
                    QuickSort(left, j);
                if (i < right)
                    QuickSort(i, right);
            }

            QuickSort(0, Count - 1);
        }
        public void Dispose()
        {
            ReturnArray(_polyItems);
            _polyItems = null!;
        }
        private void AssertCapacity(int min)
        {
            if (_polyItems.Length < min)
            {
                // All math by QRCoder should be done with fixed polynomials, so we don't need to grow the capacity.
                ThrowNotSupportedException();

                // Sample code for growing the capacity:
                //var newArray = RentArray(Math.Max(min - 1, 8) * 2); // Grow by 2x, but at least by 8
                //Array.Copy(_polyItems, newArray, _length);
                //ReturnArray(_polyItems);
                //_polyItems = newArray;
            }

            [StackTraceHidden]
            void ThrowNotSupportedException() => throw new NotSupportedException("The polynomial capacity is fixed and cannot be increased.");
        }
        [ThreadStatic]
        private static List<PolynomItem[]>? _arrayPool;
        private static PolynomItem[] RentArray(int count)
        {
            if (count <= 0)
                ThrowArgumentOutOfRangeException();

            // Search for a suitable array in the thread-local pool, if it has been initialized
            if (_arrayPool != null)
            {
                for (int i = 0; i < _arrayPool.Count; i++)
                {
                    var array = _arrayPool[i];
                    if (array.Length >= count)
                    {
                        _arrayPool.RemoveAt(i);
                        return array;
                    }
                }
            }

            // No suitable buffer found; create a new one
            return new PolynomItem[count];

            void ThrowArgumentOutOfRangeException() => throw new ArgumentOutOfRangeException(nameof(count), "The count must be a positive number.");
        }
        private static void ReturnArray(PolynomItem[] array)
        {
            if (array == null)
                return;

            // Initialize the thread-local pool if it's not already done
            _arrayPool ??= new List<PolynomItem[]>(8);

            // Add the buffer back to the pool
            _arrayPool.Add(array);
        }
        public PolynumEnumerator GetEnumerator() => new PolynumEnumerator(this);
        public struct PolynumEnumerator
        {
            private Polynom _polynom;
            private int _index;

            public PolynumEnumerator(Polynom polynom)
            {
                _polynom = polynom;
                _index = -1;
            }

            public PolynomItem Current => _polynom[_index];

            public bool MoveNext() => ++_index < _polynom.Count;
        }
    }
}
