using System.IO;
using NUnit.Framework;
using Survival.Save;
using Survival.World;

namespace Survival.Tests
{
    public class TerrainSamplerTests
    {
        [Test]
        public void SameSeed_ProducesIdenticalTerrain()
        {
            var a = new TerrainSampler(12345);
            var b = new TerrainSampler(12345);
            for (int x = -50; x < 50; x += 7)
            for (int y = -50; y < 50; y += 7)
            {
                Assert.AreEqual(a.GetBiome(x, y), b.GetBiome(x, y));
                Assert.AreEqual(a.Random01(x, y, 2), b.Random01(x, y, 2));
            }
        }

        [Test]
        public void DifferentSeeds_ProduceDifferentTerrain()
        {
            var a = new TerrainSampler(1);
            var b = new TerrainSampler(2);
            int differences = 0;
            for (int x = 0; x < 100; x += 3)
            for (int y = 0; y < 100; y += 3)
                if (a.GetBiome(x, y) != b.GetBiome(x, y)) differences++;
            Assert.Greater(differences, 50);
        }

        [Test]
        public void World_ContainsEveryBiome()
        {
            var sampler = new TerrainSampler(424242);
            var seen = new System.Collections.Generic.HashSet<Biome>();
            for (int x = -400; x < 400; x += 4)
            for (int y = -400; y < 400; y += 4)
                seen.Add(sampler.GetBiome(x, y));
            Assert.AreEqual(5, seen.Count, "Expected water, sand, grassland, forest and rocky terrain");
        }

        [Test]
        public void Random01_StaysInRange()
        {
            var sampler = new TerrainSampler(7);
            for (int i = 0; i < 1000; i++)
            {
                float v = sampler.Random01(i, -i, 3);
                Assert.GreaterOrEqual(v, 0f);
                Assert.Less(v, 1f);
            }
        }
    }

    public class SaveSystemTests
    {
        string _path;

        [SetUp]
        public void SetUp() => _path = Path.Combine(Path.GetTempPath(), $"survival-test-{System.Guid.NewGuid()}.json");

        [TearDown]
        public void TearDown()
        {
            SaveSystem.Delete(_path);
        }

        static SaveData Sample()
        {
            var data = new SaveData { worldSeed = 987, playerX = 12.5f, playerY = -3.25f };
            data.inventory.Add(new SavedSlot { slot = 0, itemId = "wood", count = 17 });
            data.inventory.Add(new SavedSlot { slot = 5, itemId = "stone", count = 3 });
            data.harvestedNodes.Add("4,-2");
            data.harvestedNodes.Add("-10,8");
            return data;
        }

        [Test]
        public void SaveAndLoad_RoundTripsAllFields()
        {
            SaveSystem.Save(Sample(), _path);
            Assert.IsTrue(SaveSystem.Exists(_path));
            Assert.IsTrue(SaveSystem.TryLoad(out var loaded, _path));

            Assert.AreEqual(987, loaded.worldSeed);
            Assert.AreEqual(12.5f, loaded.playerX);
            Assert.AreEqual(-3.25f, loaded.playerY);
            Assert.AreEqual(2, loaded.inventory.Count);
            Assert.AreEqual("stone", loaded.inventory[1].itemId);
            Assert.AreEqual(5, loaded.inventory[1].slot);
            CollectionAssert.AreEquivalent(new[] { "4,-2", "-10,8" }, loaded.harvestedNodes);
            Assert.AreEqual(SaveData.CurrentVersion, loaded.version);
        }

        [Test]
        public void Save_OverwritesExistingFile()
        {
            SaveSystem.Save(Sample(), _path);
            var second = Sample();
            second.worldSeed = 1;
            SaveSystem.Save(second, _path);
            SaveSystem.TryLoad(out var loaded, _path);
            Assert.AreEqual(1, loaded.worldSeed);
            Assert.IsFalse(File.Exists(_path + ".tmp"));
        }

        [Test]
        public void TryLoad_ReturnsFalseForMissingOrCorruptFile()
        {
            Assert.IsFalse(SaveSystem.TryLoad(out _, _path));
            File.WriteAllText(_path, "{ not json");
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;
            Assert.IsFalse(SaveSystem.TryLoad(out _, _path));
        }
    }
}
