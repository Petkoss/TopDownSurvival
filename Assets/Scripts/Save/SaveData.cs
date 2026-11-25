using System;
using System.Collections.Generic;

namespace Survival.Save
{
    /// <summary>Everything persisted between sessions. Serialized to JSON with JsonUtility.</summary>
    [Serializable]
    public class SaveData
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public string savedAtUtc;

        public int worldSeed;
        public float playerX;
        public float playerY;

        public List<SavedSlot> inventory = new List<SavedSlot>();
        public List<string> harvestedNodes = new List<string>();
    }

    [Serializable]
    public class SavedSlot
    {
        public int slot;
        public string itemId;
        public int count;
    }
}
