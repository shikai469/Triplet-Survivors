using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using TMPro;

namespace TripleTSurvival.Editor
{
    [InitializeOnLoad]
    public static class TMPFallbackFixer
    {
        static TMPFallbackFixer()
        {
            EditorApplication.delayCall += Execute;
        }

        [MenuItem("Tools/Fix TextMeshPro Fallbacks")]
        public static void Execute()
        {
            Debug.Log("[TMPFallbackFixer] Re-importing TMP font assets and settings...");

            string fallbackPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset";
            string mainFontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
            string settingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";

            AssetDatabase.ImportAsset(fallbackPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(mainFontPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(settingsPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            TMP_FontAsset fallback = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fallbackPath);
            TMP_FontAsset mainFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(mainFontPath);

            if (fallback != null)
            {
                fallback.ReadFontAssetDefinition();
            }

            if (mainFont != null)
            {
                if (mainFont.fallbackFontAssetTable == null)
                    mainFont.fallbackFontAssetTable = new List<TMP_FontAsset>();

                if (fallback != null && !mainFont.fallbackFontAssetTable.Contains(fallback))
                {
                    mainFont.fallbackFontAssetTable.Add(fallback);
                    EditorUtility.SetDirty(mainFont);
                }

                mainFont.ReadFontAssetDefinition();
            }

            string testStr = "Cài Đặt Bắt Đầu Nâng Cấp \u0110\u1EB7\u1EAF\u1EA7\u1EA5";
            if (mainFont != null)
            {
                bool allFoundWithFallback = true;
                List<char> stillMissing = new List<char>();
                foreach (char c in testStr)
                {
                    if (!mainFont.HasCharacter(c, searchFallbacks: true, tryAddCharacter: true))
                    {
                        allFoundWithFallback = false;
                        stillMissing.Add(c);
                    }
                }

                if (allFoundWithFallback)
                {
                    Debug.Log($"[TMPFallbackFixer] SUCCESS: All test characters in '{testStr}' resolved via LiberationSans SDF + Fallback!");
                }
                else
                {
                    Debug.LogWarning($"[TMPFallbackFixer] Characters still missing: {string.Join(", ", stillMissing)}");
                }
            }

            AssetDatabase.SaveAssets();
        }
    }
}
