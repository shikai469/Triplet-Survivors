#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Menu Editor tiện ích: Tự động tạo 10 ScriptableObject thẻ kỹ năng chuẩn vào Assets/UpgradeCards
/// và tự động gán vào UpgradeManager trong Scene hiện tại chỉ với 1 click!
/// </summary>
public static class SkillCardGeneratorEditor
{
    [MenuItem("TripleT/🌟 Tạo & Đồng bộ Thẻ Kỹ Năng Passive")]
    public static void GenerateAllSkillCards()
    {
        string folderPath = "Assets/UpgradeCards";
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        // Danh sách định nghĩa thẻ
        CreateOrUpdateCard(folderPath, "DamageUpCard", UpgradeData.UpgradeType.DamageUp, "Tăng Sát Thương",
            "Tăng trực tiếp sát thương đạn cho súng lục mặc định (+20% mỗi cấp).", 5,
            new string[] { "+20% Sát thương đạn", "+40% Sát thương đạn", "+60% Sát thương đạn", "+80% Sát thương đạn", "+100% Sát thương đạn (Gấp đôi!)" });

        CreateOrUpdateCard(folderPath, "PiercingShotCard", UpgradeData.UpgradeType.PiercingShot, "Đạn Xuyên Thấu",
            "Thỉnh thoảng bắn ra 1 viên đạn sniper bay xuyên thấu qua nhiều quái trên đường thẳng.", 3,
            new string[] { "Hồi 5.0s, đạn sniper uy lực xuyên quái", "Hồi 3.8s, tăng tốc độ bắn sniper", "Hồi 2.5s, xả đạn sniper dồn dập" });

        CreateOrUpdateCard(folderPath, "MultishotCard", UpgradeData.UpgradeType.Multishot, "Đạn Tõe (Multishot)",
            "Nâng cấp súng lục bắn tỏa ra nhiều hướng. Tối đa ở cấp 5 sẽ bắn tõe 3 viên. (Độc quyền nhánh A)", 5,
            new string[] { "Bắn tõe 2 viên góc hẹp", "Bắn tõe 2 viên chuẩn xác", "Bắn tõe 3 viên hình nón", "Bắn tõe 3 viên góc rộng", "Bắn tõe 3 viên uy lực tối đa" });

        CreateOrUpdateCard(folderPath, "BurstFireCard", UpgradeData.UpgradeType.BurstFire, "Đạn Liên Tục (Burst Fire)",
            "Nâng cấp súng lục bắn một chuỗi đạn liên tiếp trên cùng đường thẳng. Tối đa 3 viên. (Độc quyền nhánh B)", 5,
            new string[] { "Bắn liên tiếp chuỗi 2 viên", "Chuỗi 2 viên tốc độ cao", "Bắn liên tiếp chuỗi 3 viên", "Chuỗi 3 viên liên hoàn", "Chuỗi 3 viên uy lực xé gió" });

        CreateOrUpdateCard(folderPath, "OrbitSwordsCard", UpgradeData.UpgradeType.OrbitSwords, "Kiếm Bay (Orbit Swords)",
            "Tạo ra thanh kiếm xoay tròn quanh người chơi, gây sát thương cho quái chạm vào. Tối đa 3 kiếm.", 3,
            new string[] { "1 thanh kiếm xoay quanh người", "2 thanh kiếm xoay đối xứng 180°", "3 thanh kiếm xoay quanh người như cối xay" });

        CreateOrUpdateCard(folderPath, "SeagullAllyCard", UpgradeData.UpgradeType.SeagullAlly, "Hải Âu Trợ Chiến",
            "Triệu hồi chú chim hải âu bay kè kè bên cạnh, tự động nhả đạn hạt hướng dương về phía quái.", 3,
            new string[] { "Hải âu trợ chiến, tầm bắn 5m", "Tăng tầm bắn lên 8m, nhả đạn nhanh hơn", "Tầm bắn tối đa 12m, bao quát toàn trận" });

        CreateOrUpdateCard(folderPath, "GrenadeTossCard", UpgradeData.UpgradeType.GrenadeToss, "Ném Lựu Đạn",
            "Theo chu kỳ tự động ném ra quả lựu đạn gây nổ diện rộng (AOE) tiêu diệt đám đông quái.", 3,
            new string[] { "Ném lựu đạn mỗi 5.0s, nổ diện rộng", "Rút ngắn thời gian chờ còn 3.5s", "Ném dồn dập mỗi 2.0s quét sạch quái" });

        CreateOrUpdateCard(folderPath, "EnergyShieldCard", UpgradeData.UpgradeType.EnergyShield, "Giáp Hộ Thể",
            "Lớp giáp ảo bảo vệ đỡ đòn. Tự động hồi đầy lại sau 5s người chơi không nhận sát thương.", 3,
            new string[] { "Giáp ảo 30 HP, hồi sau 5s", "Giáp ảo 60 HP, độ bền cao", "Giáp ảo 100 HP, bảo hộ tối đa" });

        CreateOrUpdateCard(folderPath, "BouncingBoomerangCard", UpgradeData.UpgradeType.BouncingBoomerang, "Boomerang Dội Tường",
            "Ném Boomerang dội ngược khi đập tường đấu trường rồi quay về người chơi. Tối đa 3 cái.", 3,
            new string[] { "Ném 1 Boomerang dội tường", "Ném 2 Boomerang cùng lúc", "Ném 3 Boomerang càn quét chiến trường" });

        CreateOrUpdateCard(folderPath, "CoinRewardCard", UpgradeData.UpgradeType.SelectItem_Coin, "Thưởng Vàng",
            "Thưởng ngay 10 ~ 25 Vàng trực tiếp vào túi đồ trong trận.", 999, null);

        CreateOrUpdateCard(folderPath, "HealPackCard", UpgradeData.UpgradeType.SelectItem_HealPack, "Túi Cứu Thương",
            "Hồi phục ngay 100 HP (hoặc 30% Max HP) cho nhân vật.", 999, null);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Tự động gán vào UpgradeManager trong Scene
        UpgradeManager um = Object.FindFirstObjectByType<UpgradeManager>();
        if (um != null)
        {
            Undo.RecordObject(um, "Assign Skill Cards to UpgradeManager");
            um.allUpgrades.Clear();

            string[] guids = AssetDatabase.FindAssets("t:UpgradeData", new[] { folderPath });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                UpgradeData card = AssetDatabase.LoadAssetAtPath<UpgradeData>(path);
                if (card != null)
                {
                    um.allUpgrades.Add(card);
                }
            }

            EditorUtility.SetDirty(um);
            Debug.Log($"<color=green><b>[THÀNH CÔNG]</b></color> Đã tạo và đồng bộ {um.allUpgrades.Count} thẻ kỹ năng vào UpgradeManager!");
        }
        else
        {
            Debug.Log("<color=green><b>[THÀNH CÔNG]</b></color> Đã tạo xong tất cả các thẻ kỹ năng trong thư mục Assets/UpgradeCards!");
        }
    }

    private static void CreateOrUpdateCard(string folder, string fileName, UpgradeData.UpgradeType type, string title, string desc, int maxLv, string[] levelDescs)
    {
        string assetPath = $"{folder}/{fileName}.asset";
        UpgradeData card = AssetDatabase.LoadAssetAtPath<UpgradeData>(assetPath);

        if (card == null)
        {
            card = ScriptableObject.CreateInstance<UpgradeData>();
            AssetDatabase.CreateAsset(card, assetPath);
        }

        card.upgradeType = type;
        card.title = title;
        card.description = desc;
        card.maxLevel = maxLv;
        card.levelDescriptions = levelDescs;

        if (card.icon == null)
        {
            card.icon = ProceduralSkillAssets.GetSkillIcon(type);
        }

        EditorUtility.SetDirty(card);
    }
}
#endif
