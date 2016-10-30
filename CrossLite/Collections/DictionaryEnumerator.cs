using System;
using System.Collections;
using System.Collections.Generic;

namespace CrossLite.Collections
{
    internal class DictionaryEnumerator<TKey, TValue> : IDictionaryEnumerator, IDisposable
    {
        readonly IEnumerator<KeyValuePair<TKey, TValue>> impl;
        public void Dispose() { impl.Dispose(); }
        public DictionaryEnumerator(IDictionary<TKey, TValue> value)
        {
            this.impl = value.GetEnumerator();
        }
        public void Reset() { impl.Reset(); }
        public bool MoveNext() { return impl.MoveNext(); }
        public DictionaryEntry Entry
        {
            get
            {
                var pair = impl.Current;
                return new DictionaryEntry(pair.Key, pair.Value);
            }
        }
        public object Key { get { return impl.Current.Key; } }
        public object Value { get { return impl.Current.Value; } }
        public object Current { get { return Entry; } }
    }
}
