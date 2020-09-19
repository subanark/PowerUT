using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace PowerUT
{
    public class RefDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
    {
        Dictionary<TKey, Ref> dict = new Dictionary<TKey, Ref>();

        public ref TValue this[TKey key]
        {
            get
            {
                return ref dict[key].value;
            }
        }

        public ref TValue Add(TKey key, TValue value)
        {
            return  ref (dict[key] = new Ref { value = value }).value;
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return (from entry in dict
                   select new KeyValuePair<TKey, TValue>(entry.Key, entry.Value.value)).GetEnumerator();
        }

        public ref TValue Remove(TKey key)
        {
            ref TValue value = ref this[key];
            dict.Remove(key);
            return ref value;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        private class Ref
        {
            public TValue value;
        }
    }
}
