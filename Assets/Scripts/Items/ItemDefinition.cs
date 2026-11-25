using UnityEngine;

namespace Survival.Items
{
    /// <summary>Static data for one kind of item (wood, stone, ...).</summary>
    [CreateAssetMenu(fileName = "NewItem", menuName = "Survival/Item Definition")]
    public class ItemDefinition : ScriptableObject
    {
        [Tooltip("Stable identifier used by the save system. Never change it once saves exist.")]
        public string id;
        public string displayName;
        public Sprite icon;
        [Min(1)] public int maxStack = 99;
    }
}
