using System.Collections.Generic;
using UnityEngine;

namespace Game.UI
{
    [System.Serializable]
    public class AssetReferenceMap<TKey, TValue> where TValue : MonoBehaviour
    {
        [SerializeField]
        private List<AssetReferenceMapElement<TKey, TValue>> _mapContent = new List<AssetReferenceMapElement<TKey, TValue>>();

        private Dictionary<TKey, AssetReferenceComponent<TValue>> _map = new Dictionary<TKey, AssetReferenceComponent<TValue>>();

        private bool IsInitialize => _mapContent.Count == _map.Count;

        public IEnumerable<TKey> GetKeys()
        {
            if (!IsInitialize)
                Initialize();

            return _map.Keys;
        }

        public IEnumerable<AssetReferenceComponent<TValue>> GetValues()
        {
            if (!IsInitialize)
                Initialize();

            return _map.Values;
        }

        public bool TryFind(TKey key, out AssetReferenceComponent<TValue> value)
        {
            if (!IsInitialize)
                Initialize();
            return _map.TryGetValue(key, out value);
        }

        private void Initialize()
        {
            _map.Clear();
            foreach (var item in _mapContent)
            {
                _map.Add(item.Key, item.Reference);
            }
        }
    }

    [System.Serializable]
    public class AssetReferenceMapElement<TKey, TValue> where TValue : MonoBehaviour
    {
        public TKey Key;
        public AssetReferenceComponent<TValue> Reference;
    }
}