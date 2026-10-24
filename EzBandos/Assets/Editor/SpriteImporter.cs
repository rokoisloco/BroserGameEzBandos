
using UnityEditor;
using UnityEngine;

public class SpriteImporter : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (!assetPath.Contains("Resources/Sprites")) return;
        var t = (TextureImporter)assetImporter;
        t.textureType = TextureImporterType.Sprite; t.spriteImportMode = SpriteImportMode.Single;
        t.spritePixelsPerUnit = 32; t.filterMode = FilterMode.Point; t.mipmapEnabled = false;
        t.textureCompression = TextureImporterCompression.Uncompressed;
        if (assetPath.Contains("cannon_barrel"))
        {
            var s = new TextureImporterSettings(); t.ReadTextureSettings(s);
            s.spriteAlignment = (int)SpriteAlignment.Custom; s.spritePivot = new Vector2(.15f, .5f); t.SetTextureSettings(s);
        }
    }
}