using System.Collections.Generic;
using UnityEngine;

namespace Survival.Items
{
    /// <summary>Registry of every item, used to resolve saved item ids back to definitions.</summary>
    [CreateAssetMenu(fileName = "ItemDatabase", menuName = "Survival/Item Database")]
    public class ItemDatabase : ScriptableObject
    {
        public List<ItemDefinition> items = new List<ItemDefinition>();

        Dictionary<string, ItemDefinition> _lookup;

        public ItemDefinition Get(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (_lookup == null || _lookup.Count != items.Count) BuildLookup();
            return _lookup.TryGetValue(id, out var item) ? item : null;
        }

        void OnEnable() => _lookup = null;

        void BuildLookup()
        {
            _lookup = new Dictionary<string, ItemDefinition>();
            foreach (var item in items)
            {
                if (item == null || string.IsNullOrEmpty(item.id)) continue;
                if (!_lookup.TryAdd(item.id, item))
                    Debug.LogWarning($"Duplicate item id '{item.id}' in {name}", this);
            }
        }
    }
}
