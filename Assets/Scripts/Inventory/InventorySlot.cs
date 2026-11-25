using System;
using Survival.Items;

namespace Survival.InventorySystem
{
    [Serializable]
    public class InventorySlot
    {
        public ItemDefinition item;
        public int count;

        public bool IsEmpty => item == null || count <= 0;

        public void Clear()
        {
            item = null;
            count = 0;
        }
    }
}
