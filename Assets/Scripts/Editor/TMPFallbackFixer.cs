using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using TMPro;

namespace TripleTSurvival.Editor
{
    /// <summary>
    /// Công cụ kiểm tra và liên kết Font Fallback tiếng Việt cho TextMeshPro.
    /// Chỉ kích hoạt khi người dùng chọn menu Tools -> Fix TextMeshPro Fallbacks,
    /// tránh việc tự động ép re-import mỗi khi compile code gây lỗi NativeFormatImporter.
    /// </summary>
    public static class TMPFallbackFixer
    {
        [MenuItem("Tools/Fix TextMeshPro Fallbacks")]
        public static void Execute()
        {
            Debug.Log("[TMPFallbackFixer] Đang kiểm tra cấu hình font TextMeshPro Fallback...");

            string fallbackPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset";
            string mainFontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

            TMP_FontAsset fallback = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fallbackPath);
            TMP_FontAsset mainFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(mainFontPath);

            if (fallback == null || mainFont == null)
            {
                Debug.LogWarning("[TMPFallbackFixer] Không tìm thấy font asset tại đường dẫn mặc định.");
                return;
            }

            if (mainFont.fallbackFontAssetTable == null)
                mainFont.fallbackFontAssetTable = new List<TMP_FontAsset>();

            bool changed = false;
            if (!mainFont.fallbackFontAssetTable.Contains(fallback))
            {
                mainFont.fallbackFontAssetTable.Add(fallback);
                EditorUtility.SetDirty(mainFont);
                changed = true;
            }

            if (changed)
            {
                AssetDatabase.SaveAssets();
                Debug.Log("[TMPFallbackFixer] Đã cập nhật thành công Fallback font vào LiberationSans SDF!");
            }
            else
            {
                Debug.Log("[TMPFallbackFixer] Font Fallback tiếng Việt đã được cấu hình và liên kết hoàn hảo từ trước!");
            }
        }
    }
}
