using UnityEditor;
using UnityEngine;

namespace Survival.EditorTools
{
    /// <summary>Pixel-art import settings for everything in Assets/Art/Sprites (16 PPU, point filtering).</summary>
    public class SpriteImportSettings : AssetPostprocessor
    {
        public const string SpriteFolder = "Assets/Art/Sprites/";
        public const int PixelsPerUnit = 16;

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(SpriteFolder)) return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteExtrude = 0;
            // World objects stand on their bottom edge so Y-sorting looks right; tiles and icons are centred.
            settings.spriteAlignment = (int)(IsWorldObject(assetPath) ? SpriteAlignment.BottomCenter : SpriteAlignment.Center);
            importer.SetTextureSettings(settings);
        }

        static bool IsWorldObject(string path)
        {
            string file = System.IO.Path.GetFileNameWithoutExtension(path);
            return file.StartsWith("tree_") || file == "rock" || file == "mushroom" || file == "player";
        }
    }
}
