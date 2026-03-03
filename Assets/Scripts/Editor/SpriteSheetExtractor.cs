using UnityEngine;
using UnityEditor;
using System.IO;

namespace AvatarChat.Editor
{
    public class SpriteSheetExtractor : EditorWindow
    {
        [MenuItem("Assets/AvatarChat/Extract Sprites to PNG")]
        public static void ExtractSprites()
        {
            Object[] selectedObjects = Selection.objects;

            foreach (Object obj in selectedObjects)
            {
                if (obj is Texture2D texture)
                {
                    string path = AssetDatabase.GetAssetPath(texture);
                    TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

                    if (importer == null || importer.spriteImportMode != SpriteImportMode.Multiple)
                    {
                        continue;
                    }

                    Extract(texture, path);
                }
            }
            AssetDatabase.Refresh();
        }

        private static void Extract(Texture2D sourceTexture, string sourcePath)
        {
            string directory = Path.GetDirectoryName(sourcePath);
            string exportFolder = Path.Combine(directory, sourceTexture.name + "_Extracted");

            if (!Directory.Exists(exportFolder))
                Directory.CreateDirectory(exportFolder);

            Object[] sprites = AssetDatabase.LoadAllAssetsAtPath(sourcePath);

            Texture2D readableTexture = GetReadableTextureCorrectColor(sourceTexture);

            foreach (Object obj in sprites)
            {
                if (obj is Sprite sprite)
                {
                    int width = (int)sprite.rect.width;
                    int height = (int)sprite.rect.height;

                    Texture2D extracted = new Texture2D(width, height, TextureFormat.RGBA32, false);

                    Color[] pixels = readableTexture.GetPixels(
                        (int)sprite.rect.x,
                        (int)sprite.rect.y,
                        width,
                        height
                    );

                    extracted.SetPixels(pixels);
                    extracted.Apply();

                    byte[] pngData = extracted.EncodeToPNG();
                    File.WriteAllBytes(Path.Combine(exportFolder, sprite.name + ".png"), pngData);

                    DestroyImmediate(extracted);
                }
            }

            DestroyImmediate(readableTexture);
            Debug.Log($"Готово! Спрайты извлечены в: {exportFolder}");
        }

        private static Texture2D GetReadableTextureCorrectColor(Texture2D source)
        {
            RenderTexture rt = RenderTexture.GetTemporary(
                source.width,
                source.height,
                0,
                RenderTextureFormat.Default,
                RenderTextureReadWrite.sRGB
            );

            Graphics.Blit(source, rt);

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = rt;

            Texture2D readable = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            readable.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            readable.Apply();

            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);

            return readable;
        }
    }
}