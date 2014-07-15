using System.Collections.Generic;

namespace Ketchup.Utility
{
    public sealed class ConfigDictionary : IDictionary<string, string>, IConfigNode
    {

        private readonly IDictionary<string, string> _dictionary = new Dictionary<string, string>();

        #region IDictionary

        public ICollection<string> Keys
        {
            get { return _dictionary.Keys; }
        }

        public ICollection<string> Values
        {
            get { return _dictionary.Values; }
        }

        public string this[string key]
        {
            get
            {
                return _dictionary[key];
            }
            set
            {
                _dictionary[key] = value;
            }
        }

        public int Count
        {
            get { return _dictionary.Count; }
        }

        public bool IsReadOnly
        {
            get { return _dictionary.IsReadOnly; }
        }

        public void Add(string key, string value)
        {
            _dictionary.Add(key, value);
        }

        public bool ContainsKey(string key)
        {
            return _dictionary.ContainsKey(key);
        }

        public bool Remove(string key)
        {
            return _dictionary.Remove(key);
        }

        public bool TryGetValue(string key, out string value)
        {
            return _dictionary.TryGetValue(key, out value);
        }

        public void Add(KeyValuePair<string, string> item)
        {
            _dictionary.Add(item);
        }

        public void Clear()
        {
            _dictionary.Clear();
        }

        public bool Contains(KeyValuePair<string, string> item)
        {
            return _dictionary.Contains(item);
        }

        public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
        {
            _dictionary.CopyTo(array, arrayIndex);
        }

        public bool Remove(KeyValuePair<string, string> item)
        {
            return _dictionary.Remove(item);
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return _dictionary.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _dictionary.GetEnumerator();
        }

        #endregion

        #region IConfigNode

        public void Load(ConfigNode node)
        {
            foreach (ConfigNode.Value v in node.values)
            {
                Add(v.name, v.value);
            }
        }

        public void Save(ConfigNode node)
        {
            foreach (var kv in _dictionary)
            {
                node.AddValue(kv.Key, kv.Value);
            }
        }

        #endregion
    }
}
