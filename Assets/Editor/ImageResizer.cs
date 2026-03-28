using UnityEditor;
using UnityEngine;

/// <summary>
/// Resizes sprite textures to target resolution
/// </summary>
public static class ImageResizer
{
    [MenuItem("CS4483/🔧 Resize Pickup Sprites to 40x40")]
    public static void ResizePickupSprites()
    {
        Debug.Log("[ImageResizer] Resizing pickup sprites to 40x40...");
        
        ResizeTexture("Assets/Sprites/sExperience.png", 40, 40);
        ResizeTexture("Assets/Sprites/sMedkit.png", 40, 40);
        // Obstacle art is sBox.png (run Sprite Setup for import settings; do not force-resize here)
        
        AssetDatabase.Refresh();
        Debug.Log("[ImageResizer] ✓ Pickup and obstacle sprites resized to 40x40!");
    }
    
    private static void ResizeTexture(string path, int targetWidth, int targetHeight)
    {
        // Load the texture
        Texture2D originalTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (originalTexture == null)
        {
            Debug.LogError($"[ImageResizer] Could not load texture at {path}");
            return;
        }
        
        // Make texture readable
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.isReadable = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
        
        // Reload after making readable
        originalTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        
        // Create new texture with target size
        Texture2D resizedTexture = new Texture2D(targetWidth, targetHeight, TextureFormat.RGBA32, false);
        resizedTexture.filterMode = FilterMode.Point; // Pixel-perfect scaling
        
        // Resize using bilinear filtering
        RenderTexture rt = RenderTexture.GetTemporary(targetWidth, targetHeight);
        rt.filterMode = FilterMode.Bilinear;
        
        RenderTexture.active = rt;
        Graphics.Blit(originalTexture, rt);
        
        resizedTexture.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
        resizedTexture.Apply();
        
        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);
        
        // Save as PNG
        byte[] pngData = resizedTexture.EncodeToPNG();
        System.IO.File.WriteAllBytes(path, pngData);
        
        Debug.Log($"[ImageResizer] Resized {path} to {targetWidth}x{targetHeight}");
        
        // Cleanup
        Object.DestroyImmediate(resizedTexture);
    }
}
