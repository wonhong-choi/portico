using System;
using System.Collections;
using System.Collections.Generic;

namespace PorticoRti1516e.Encoding
{
    // Variable-count array of homogeneous IDataElements: 4-byte BE count prefix followed
    // by the elements, self-describing on the wire (a decoder with no prior knowledge of
    // size can reconstruct the array). Port of HLA1516eVariableArray<T> (Java). Requires
    // a factory (unlike Java, which tolerates a null factory as an edge case its decode()
    // separately defends against) - simpler and fail-fast to require it up front.
    public sealed class HLAvariableArray<T> : DataElementBase, IEnumerable<T> where T : IDataElement
    {
        private readonly IDataElementFactory<T> _factory;
        private readonly List<T> _elements;
        private int _boundary = -1;

        public HLAvariableArray(IDataElementFactory<T> factory, params T[] provided)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _elements = new List<T>(provided);
        }

        public void AddElement(T element)
        {
            _elements.Add(element);
            _boundary = -1;
        }

        // Resizes to newSize, trimming from the end or growing via the factory - direct
        // port of Java's resize().
        public void Resize(int newSize)
        {
            if (newSize < _elements.Count)
            {
                while (newSize < _elements.Count)
                    _elements.RemoveAt(_elements.Count - 1);
            }
            else if (newSize > _elements.Count)
            {
                while (newSize > _elements.Count)
                    _elements.Add(_factory.CreateElement(_elements.Count));
            }
        }

        public int Size => _elements.Count;

        public T Get(int index) => _elements[index];

        public IEnumerator<T> GetEnumerator()
        {
            _boundary = -1;
            return _elements.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public override int GetOctetBoundary()
        {
            if (_boundary == -1)
            {
                int calculated = 4;
                foreach (var element in _elements)
                    calculated = Math.Max(calculated, element.GetOctetBoundary());

                // If the list is empty we need to create the default type of element and
                // pull the boundary from there - minimum is always 4 (to hold the length
                // prefix itself).
                if (_elements.Count == 0)
                    calculated = Math.Max(calculated, _factory.CreateElement(0).GetOctetBoundary());

                _boundary = calculated;
            }

            return _boundary;
        }

        public override int GetEncodedLength()
        {
            int length = 4;
            foreach (var element in _elements)
            {
                while (length % element.GetOctetBoundary() != 0)
                    length++;

                length += element.GetEncodedLength();
            }

            return length;
        }

        public override void Encode(ByteWrapper byteWrapper)
        {
            byteWrapper.Align(GetOctetBoundary());
            byteWrapper.PutInt(_elements.Count);
            foreach (var element in _elements)
                element.Encode(byteWrapper);
        }

        public override void Decode(ByteWrapper byteWrapper)
        {
            byteWrapper.Align(GetOctetBoundary());

            // Get the size and make sure there is enough data to feed us.
            int size = byteWrapper.GetInt();
            // NOTE: preserved from the Java source verbatim (its own comment admits this
            // is checking the wrong unit: `size` is an element count, not a byte length,
            // so this is a loose/approximate early sanity check, not a precise bounds
            // check - harmless in practice since size is always <= remaining byte length
            // for reasonably-sized elements, not a decode-correctness bug like the
            // HLAunicodeString one).
            byteWrapper.Verify(size);

            Resize(size);
            foreach (var element in _elements)
                element.Decode(byteWrapper);
        }
    }
}
