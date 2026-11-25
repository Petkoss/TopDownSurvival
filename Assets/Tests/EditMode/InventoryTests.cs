using NUnit.Framework;
using Survival.InventorySystem;
using Survival.Items;
using UnityEngine;

namespace Survival.Tests
{
    public class InventoryTests
    {
        GameObject _go;
        Inventory _inventory;
        ItemDefinition _wood;
        ItemDefinition _stone;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("Inventory");
            _inventory = _go.AddComponent<Inventory>();
            _inventory.Configure(4);
            _wood = Item("wood", 10);
            _stone = Item("stone", 5);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_wood);
            Object.DestroyImmediate(_stone);
        }

        static ItemDefinition Item(string id, int maxStack)
        {
            var item = ScriptableObject.CreateInstance<ItemDefinition>();
            item.id = id;
            item.displayName = id;
            item.maxStack = maxStack;
            return item;
        }

        [Test]
        public void Add_FillsExistingStackBeforeNewSlot()
        {
            _inventory.Add(_wood, 4);
            _inventory.Add(_wood, 3);
            Assert.AreEqual(7, _inventory.GetSlot(0).count);
            Assert.IsTrue(_inventory.GetSlot(1).IsEmpty);
        }

        [Test]
        public void Add_SplitsAcrossStacksAtMaxStack()
        {
            int leftover = _inventory.Add(_wood, 25);
            Assert.AreEqual(0, leftover);
            Assert.AreEqual(10, _inventory.GetSlot(0).count);
            Assert.AreEqual(10, _inventory.GetSlot(1).count);
            Assert.AreEqual(5, _inventory.GetSlot(2).count);
            Assert.AreEqual(25, _inventory.Count(_wood));
        }

        [Test]
        public void Add_ReturnsLeftoverWhenFull()
        {
            int leftover = _inventory.Add(_stone, 23);
            Assert.AreEqual(3, leftover);
            Assert.AreEqual(20, _inventory.Count(_stone));
            Assert.IsFalse(_inventory.CanAdd(_stone, 1));
            Assert.IsFalse(_inventory.CanAdd(_wood, 1));
        }

        [Test]
        public void Remove_TakesFromStacksAndRejectsTooMany()
        {
            _inventory.Add(_wood, 15);
            Assert.IsFalse(_inventory.Remove(_wood, 16));
            Assert.AreEqual(15, _inventory.Count(_wood));
            Assert.IsTrue(_inventory.Remove(_wood, 12));
            Assert.AreEqual(3, _inventory.Count(_wood));
        }

        [Test]
        public void MoveOrSwap_SwapsDifferentItemsAndMergesSameItems()
        {
            _inventory.SetSlot(0, _wood, 6);
            _inventory.SetSlot(1, _stone, 2);
            _inventory.MoveOrSwap(0, 1);
            Assert.AreEqual(_stone, _inventory.GetSlot(0).item);
            Assert.AreEqual(_wood, _inventory.GetSlot(1).item);

            _inventory.SetSlot(2, _wood, 7);
            _inventory.MoveOrSwap(2, 1); // 7 onto 6, max 10 -> 10 and 3 left
            Assert.AreEqual(10, _inventory.GetSlot(1).count);
            Assert.AreEqual(3, _inventory.GetSlot(2).count);
        }

        [Test]
        public void Changed_IsRaisedOnAdd()
        {
            int calls = 0;
            _inventory.Changed += () => calls++;
            _inventory.Add(_wood, 1);
            Assert.AreEqual(1, calls);
        }
    }
}
