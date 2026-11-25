using System;
using Survival.Items;
using UnityEngine;

namespace Survival.InventorySystem
{
    /// <summary>Fixed-size, slot-based inventory with stacking.</summary>
    public class Inventory : MonoBehaviour
    {
        [SerializeField, Min(1)] int slotCount = 24;

        InventorySlot[] _slots;

        /// <summary>Raised whenever any slot changes.</summary>
        public event Action Changed;

        public int SlotCount => slotCount;

        InventorySlot[] Slots
        {
            get
            {
                if (_slots == null || _slots.Length != slotCount)
                {
                    _slots = new InventorySlot[slotCount];
                    for (int i = 0; i < slotCount; i++) _slots[i] = new InventorySlot();
                }
                return _slots;
            }
        }

        public InventorySlot GetSlot(int index) => Slots[index];

        /// <summary>Resizes the inventory (clears it). Mainly for setup and tests.</summary>
        public void Configure(int slots)
        {
            slotCount = Mathf.Max(1, slots);
            _slots = null;
            Changed?.Invoke();
        }

        public int Count(ItemDefinition item)
        {
            int total = 0;
            foreach (var slot in Slots)
                if (slot.item == item) total += slot.count;
            return total;
        }

        /// <summary>How many of <paramref name="item"/> could still be added.</summary>
        public int SpaceFor(ItemDefinition item)
        {
            if (item == null) return 0;
            int space = 0;
            foreach (var slot in Slots)
            {
                if (slot.IsEmpty) space += item.maxStack;
                else if (slot.item == item) space += Mathf.Max(0, item.maxStack - slot.count);
            }
            return space;
        }

        public bool CanAdd(ItemDefinition item, int amount) => SpaceFor(item) >= amount;

        /// <summary>Adds items, filling existing stacks first. Returns the amount that did not fit.</summary>
        public int Add(ItemDefinition item, int amount)
        {
            if (item == null || amount <= 0) return Mathf.Max(0, amount);
            int remaining = amount;

            foreach (var slot in Slots)
            {
                if (remaining == 0) break;
                if (slot.IsEmpty || slot.item != item || slot.count >= item.maxStack) continue;
                int moved = Mathf.Min(remaining, item.maxStack - slot.count);
                slot.count += moved;
                remaining -= moved;
            }

            foreach (var slot in Slots)
            {
                if (remaining == 0) break;
                if (!slot.IsEmpty) continue;
                int moved = Mathf.Min(remaining, item.maxStack);
                slot.item = item;
                slot.count = moved;
                remaining -= moved;
            }

            if (remaining != amount) Changed?.Invoke();
            return remaining;
        }

        /// <summary>Removes items if enough are present. Returns false (and changes nothing) otherwise.</summary>
        public bool Remove(ItemDefinition item, int amount)
        {
            if (item == null || amount <= 0) return false;
            if (Count(item) < amount) return false;

            int remaining = amount;
            for (int i = Slots.Length - 1; i >= 0 && remaining > 0; i--)
            {
                var slot = Slots[i];
                if (slot.item != item) continue;
                int taken = Mathf.Min(remaining, slot.count);
                slot.count -= taken;
                remaining -= taken;
                if (slot.count <= 0) slot.Clear();
            }

            Changed?.Invoke();
            return true;
        }

        /// <summary>Moves the stack in <paramref name="from"/> onto <paramref name="to"/>: merges equal items, swaps otherwise.</summary>
        public void MoveOrSwap(int from, int to)
        {
            if (from == to || from < 0 || to < 0 || from >= Slots.Length || to >= Slots.Length) return;
            var a = Slots[from];
            var b = Slots[to];
            if (a.IsEmpty) return;

            if (!b.IsEmpty && b.item == a.item)
            {
                int moved = Mathf.Min(a.count, a.item.maxStack - b.count);
                b.count += moved;
                a.count -= moved;
                if (a.count <= 0) a.Clear();
            }
            else
            {
                (a.item, b.item) = (b.item, a.item);
                (a.count, b.count) = (b.count, a.count);
            }

            Changed?.Invoke();
        }

        /// <summary>Directly sets a slot's contents (used when loading a save).</summary>
        public void SetSlot(int index, ItemDefinition item, int count)
        {
            if (index < 0 || index >= Slots.Length) return;
            var slot = Slots[index];
            if (item == null || count <= 0)
            {
                slot.Clear();
            }
            else
            {
                slot.item = item;
                slot.count = Mathf.Min(count, item.maxStack);
            }
            Changed?.Invoke();
        }

        public void Clear()
        {
            foreach (var slot in Slots) slot.Clear();
            Changed?.Invoke();
        }
    }
}
