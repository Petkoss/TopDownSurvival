using System;
using System.IO;
using UnityEngine;

namespace Survival.Save
{
    /// <summary>Reads and writes <see cref="SaveData"/> as JSON on disk.</summary>
    public static class SaveSystem
    {
        public const string DefaultFileName = "savegame.json";

        public static string DefaultPath => Path.Combine(Application.persistentDataPath, DefaultFileName);

        public static string ToJson(SaveData data) => JsonUtility.ToJson(data, true);

        public static SaveData FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            var data = JsonUtility.FromJson<SaveData>(json);
            if (data == null) return null;
            data.inventory ??= new System.Collections.Generic.List<SavedSlot>();
            data.harvestedNodes ??= new System.Collections.Generic.List<string>();
            return data;
        }

        public static bool Exists(string path = null) => File.Exists(path ?? DefaultPath);

        /// <summary>Writes atomically: a crash mid-write can't corrupt the previous save.</summary>
        public static void Save(SaveData data, string path = null)
        {
            path ??= DefaultPath;
            data.version = SaveData.CurrentVersion;
            data.savedAtUtc = DateTime.UtcNow.ToString("o");

            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

            string temp = path + ".tmp";
            File.WriteAllText(temp, ToJson(data));
            if (File.Exists(path)) File.Replace(temp, path, null);
            else File.Move(temp, path);
        }

        public static bool TryLoad(out SaveData data, string path = null)
        {
            path ??= DefaultPath;
            data = null;
            if (!File.Exists(path)) return false;
            try
            {
                data = FromJson(File.ReadAllText(path));
                if (data == null) return false;
                if (data.version > SaveData.CurrentVersion)
                    Debug.LogWarning($"Save file version {data.version} is newer than supported ({SaveData.CurrentVersion}).");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Could not read save file at {path}: {e.Message}");
                data = null;
                return false;
            }
        }

        public static void Delete(string path = null)
        {
            path ??= DefaultPath;
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
