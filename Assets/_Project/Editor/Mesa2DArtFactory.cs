using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SuecaOnline.EditorTools
{
    /// <summary>
    /// Gera proceduralmente os recursos visuais da mesa (sprites 9-slice, textura de
    /// feltro e materiais TMP com contorno + underlay). Deterministico e idempotente:
    /// nenhum asset externo e necessario e reexecutar nao corrompe o resultado.
    /// </summary>
    public static class Mesa2DArtFactory
    {
        public const string ArtFolder = "Assets/_Project/Art";

        public const string RoundedPanelPath = ArtFolder + "/RoundedPanel.png";
        public const string CardFacePath = ArtFolder + "/CardFace.png";
        public const string FeltGrainPath = ArtFolder + "/FeltGrain.png";
        public const string VignettePath = ArtFolder + "/Vignette.png";
        public const string RankMaterialPath = ArtFolder + "/CardRankStyle.mat";
        public const string SuitMaterialPath = ArtFolder + "/CardSuitStyle.mat";

        private const string DropShadowPresetPath =
            "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Drop Shadow.mat";

        private const int RoundedPanelSize = 64;
        private const float RoundedPanelRadius = 20f;
        private const int RoundedPanelBorder = 24;

        private const int CardFaceSize = 128;
        private const float CardFaceRadius = 26f;
        private const float CardFaceBorderWidth = 3f;
        private const int CardFaceSliceBorder = 32;

        private const int FeltGrainSize = 256;
        private const int VignetteSize = 256;

        private static readonly Color CardFaceFill = new Color(1f, 1f, 1f, 1f);
        private static readonly Color CardFaceBorder = new Color(0.78f, 0.80f, 0.85f, 1f);

        /// <summary>Sprite branco com cantos arredondados e borda 9-slice (tintavel).</summary>
        public static Sprite GetRoundedPanel()
        {
            return CreateOrLoadSprite(
                RoundedPanelPath,
                RoundedPanelSize,
                RoundedPanelBorder,
                TextureWrapMode.Clamp,
                100f,
                (x, y) => RoundedRectPixel(x, y, RoundedPanelSize, RoundedPanelRadius, 0f, Color.white, Color.white));
        }

        /// <summary>Sprite da frente da carta: retangulo arredondado com borda clara.</summary>
        public static Sprite GetCardFace()
        {
            return CreateOrLoadSprite(
                CardFacePath,
                CardFaceSize,
                CardFaceSliceBorder,
                TextureWrapMode.Clamp,
                100f,
                (x, y) => RoundedRectPixel(x, y, CardFaceSize, CardFaceRadius, CardFaceBorderWidth, CardFaceFill, CardFaceBorder));
        }

        /// <summary>Textura de grao de feltro, sem costura, repetivel (Image.Type.Tiled).</summary>
        public static Sprite GetFeltGrain()
        {
            return CreateOrLoadSprite(
                FeltGrainPath,
                FeltGrainSize,
                0,
                TextureWrapMode.Repeat,
                100f,
                (x, y) => FeltGrainPixel(x, y, FeltGrainSize));
        }

        /// <summary>Vinheta radial (escurece as bordas) para profundidade da mesa.</summary>
        public static Sprite GetVignette()
        {
            return CreateOrLoadSprite(
                VignettePath,
                VignetteSize,
                0,
                TextureWrapMode.Clamp,
                100f,
                (x, y) => VignettePixel(x, y, VignetteSize));
        }

        /// <summary>Material TMP do valor: contorno escuro + underlay para legibilidade.</summary>
        public static Material GetRankMaterial()
        {
            return CreateOrLoadTextMaterial(
                RankMaterialPath,
                new Color(0.09f, 0.09f, 0.12f, 1f),
                0.12f,
                new Color(0.05f, 0.05f, 0.07f, 0.55f),
                0.5f,
                -0.5f);
        }

        /// <summary>Material TMP do naipe: contorno escuro + underlay para legibilidade.</summary>
        public static Material GetSuitMaterial()
        {
            return CreateOrLoadTextMaterial(
                SuitMaterialPath,
                new Color(0.09f, 0.09f, 0.12f, 1f),
                0.18f,
                new Color(0.05f, 0.05f, 0.07f, 0.55f),
                0.7f,
                -0.7f);
        }

        private static Material CreateOrLoadTextMaterial(
            string path,
            Color outlineColor,
            float outlineWidth,
            Color underlayColor,
            float underlayOffsetX,
            float underlayOffsetY)
        {
            EnsureFolder(ArtFolder);

            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Material preset = AssetDatabase.LoadAssetAtPath<Material>(DropShadowPresetPath);
                if (preset == null)
                {
                    Debug.LogWarning("[Mesa2DArtFactory] Preset TMP de sombra nao encontrado: " + DropShadowPresetPath);
                    return null;
                }

                material = new Material(preset);
                material.name = Path.GetFileNameWithoutExtension(path);
                AssetDatabase.CreateAsset(material, path);
            }

            material.EnableKeyword("OUTLINE_ON");
            material.EnableKeyword("UNDERLAY_ON");
            material.SetColor("_OutlineColor", outlineColor);
            material.SetFloat("_OutlineWidth", outlineWidth);
            material.SetColor("_UnderlayColor", underlayColor);
            material.SetFloat("_UnderlayOffsetX", underlayOffsetX);
            material.SetFloat("_UnderlayOffsetY", underlayOffsetY);
            material.SetFloat("_UnderlayDilate", 0.1f);
            material.SetFloat("_UnderlaySoftness", 0.1f);

            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            return material;
        }

        private static Sprite CreateOrLoadSprite(
            string path,
            int size,
            int border,
            TextureWrapMode wrapMode,
            float pixelsPerUnit,
            Func<int, int, Color32> pixelFactory)
        {
            EnsureFolder(ArtFolder);

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    pixels[(y * size) + x] = pixelFactory(x, y);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            string absolutePath = Path.Combine(Directory.GetParent(UnityEngine.Application.dataPath).FullName, path);
            File.WriteAllBytes(absolutePath, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = pixelsPerUnit;
                importer.spriteBorder = new Vector4(border, border, border, border);
                importer.filterMode = FilterMode.Bilinear;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.wrapMode = wrapMode;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Color32 RoundedRectPixel(
            int x,
            int y,
            int size,
            float radius,
            float borderWidth,
            Color fill,
            Color border)
        {
            float half = size * 0.5f;
            float px = (x + 0.5f) - half;
            float py = (y + 0.5f) - half;

            float dx = Mathf.Abs(px) - (half - radius);
            float dy = Mathf.Abs(py) - (half - radius);

            float outside = Mathf.Sqrt((Mathf.Max(dx, 0f) * Mathf.Max(dx, 0f)) + (Mathf.Max(dy, 0f) * Mathf.Max(dy, 0f)));
            float inside = Mathf.Min(Mathf.Max(dx, dy), 0f);
            float distance = outside + inside - radius;

            float outerAlpha = Mathf.Clamp01(0.5f - distance);
            if (outerAlpha <= 0f)
            {
                return new Color32(0, 0, 0, 0);
            }

            float innerAlpha = borderWidth > 0f ? Mathf.Clamp01(0.5f - (distance + borderWidth)) : 1f;
            Color rgb = borderWidth > 0f ? Color.Lerp(border, fill, innerAlpha) : fill;

            return new Color32(
                (byte)Mathf.RoundToInt(rgb.r * 255f),
                (byte)Mathf.RoundToInt(rgb.g * 255f),
                (byte)Mathf.RoundToInt(rgb.b * 255f),
                (byte)Mathf.RoundToInt(Mathf.Clamp01(outerAlpha) * 255f));
        }

        private static Color32 FeltGrainPixel(int x, int y, int size)
        {
            float nx = (float)x / size;
            float ny = (float)y / size;
            float twoPi = Mathf.PI * 2f;

            float waves =
                (Mathf.Sin((twoPi * ((nx * 3f) + (ny * 1f))) + 0.4f) * 0.5f) +
                (Mathf.Sin((twoPi * ((nx * 1f) - (ny * 4f))) + 1.3f) * 0.3f) +
                (Mathf.Sin((twoPi * ((nx * 7f) + (ny * 5f))) + 2.1f) * 0.2f);

            float grain = Hash01(x, y, 7717);
            float value = ((waves * 0.5f) + 0.5f) * 0.6f + (grain * 0.4f);
            value = Mathf.Clamp01(value);

            byte alpha = (byte)Mathf.RoundToInt(value * 0.14f * 255f);
            return new Color32(255, 255, 255, alpha);
        }

        private static Color32 VignettePixel(int x, int y, int size)
        {
            float half = size * 0.5f;
            float dx = ((x + 0.5f) - half) / half;
            float dy = ((y + 0.5f) - half) / half;
            float d = Mathf.Clamp01(Mathf.Sqrt((dx * dx) + (dy * dy)));

            float alpha = Mathf.Clamp01((d - 0.55f) / 0.45f);
            alpha *= alpha;

            return new Color32(0, 0, 0, (byte)Mathf.RoundToInt(alpha * 0.55f * 255f));
        }

        private static float Hash01(int x, int y, int seed)
        {
            unchecked
            {
                uint h = (uint)((x * 374761393) + (y * 668265263) + (seed * 362437));
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0xFFFFFFu) / (float)0xFFFFFFu;
            }
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string leaf = Path.GetFileName(path);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
