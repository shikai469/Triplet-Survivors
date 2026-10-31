# BÁO CÁO TỔNG HỢP COMMIT TRONG TUẦN
**Dự án:** TripleT Survival (Triplet-Survivors)  
**Thời gian tổng hợp:** 7 ngày qua (24/10/2026 – 30/10/2026)  
**Nhánh làm việc:** `main`  
**Người thực hiện chính:** `shikai469` (`nhh1822@gmail.com`)  

---

## 1. TỔNG QUAN TUẦN LÀM VIỆC

Trong tuần qua, dự án đã có bước tiến lớn về cả **Gameplay**, **Giao diện (UI/UX)**, **Hệ thống Kỹ năng (Skill System)** và **Công cụ Editor (Tools)**. 

- **Tổng số Commit:** 5 commit lớn.
- **Tổng số tệp thay đổi:** 150+ tệp (bao gồm C# scripts, Scenes, ScriptableObjects, Prefabs, Sprites và Editor Tools).
- **Trọng tâm chính:**
  1. Xây dựng và hoàn thiện hệ thống **Kỹ năng bị động & vũ khí bổ trợ (Passive Skills)**.
  2. Tái cấu trúc thư mục mã nguồn theo kiến trúc module sạch sẽ (Clean Architecture).
  3. Xây dựng màn chọn ải **Level Select** theo phong cách **Candy Crush Map** với đường nối mượt mà và bản xem trước màn chơi.
  4. Nâng cấp mỹ thuật toàn diện: bộ icon kỹ năng mới, component vẽ viền bo góc hiện đại (`UIRoundedBorder`), và hoàn thiện UI Settings/Upgrade.

---

## 2. BẢNG TỔNG HỢP COMMIT THEO THỜI GIAN

| Thời gian (Giờ / Ngày) | Mã Commit | Người thực hiện | Tiêu đề Commit | Phạm vi trọng tâm |
| :--- | :---: | :---: | :--- | :--- |
| **21:54 - 28/10/2026** | `9906a85` | `shikai469` | **Add Passive Skills** | Tái cấu trúc thư mục, triển khai hệ thống 6 Passive Skills, Overdrive và Skill Card Generator |
| **14:42 - 29/10/2026** | `d0e6aac` | `shikai469` | **Fix icon** | Setup icon nâng cấp tự động, Level Path Renderer, Styler giao diện cài đặt và procedural icons |
| **22:32 - 29/10/2026** | `97df9fc` | `shikai469` | **Update LevelSelectScene** | Map Candy Crush 900x1500, Map Previews, bộ icon âm thanh và công cụ sinh bản đồ tự động |
| **12:54 - 30/10/2026** | `97f8b47` | `shikai469` | **UIRoundedBorder** | Viết component UI bo góc tròn `UIRoundedBorder`, chuẩn hóa toàn bộ UI trên 5 Scenes |
| **15:16 - 30/10/2026** | `90abdb6` | `shikai469` | **Upgrade Skill Pool** | Tích hợp bộ Sprite Icons mới, cân bằng gameplay kỹ năng, nâng cấp Pause & Settings |

---

## 3. CHI TIẾT TỪNG NGÀY VÀ TÍNH NĂNG ĐÃ HOÀN THÀNH

### 📅 Ngày 28/10/2026 (Thứ Tư)
#### Commit `9906a85`: *Add Passive Skills*
- **Người thực hiện:** `shikai469`
- **Các tính năng & nội dung hoàn thành:**
  - **Tái cấu trúc mã nguồn toàn diện:** Di chuyển và gom nhóm toàn bộ script vào các thư mục chức năng: `Collectibles/`, `Combat/`, `Core/`, `Enemies/`, `Environment/`, `Player/`, `Skills/`, `UI/`, `Upgrades/`.
  - **Hệ thống Passive Skill & Overdrive:**
    - `PassiveSkillController.cs`: Quản lý danh sách các kỹ năng bị động của người chơi trong trận đấu.
    - `OverdriveManager.cs`: Cơ chế bộc phá/cường hóa sức mạnh khi kích hoạt kỹ năng đặc biệt.
  - **Phát triển 6 Kỹ năng mới:**
    1. `BouncingBoomerangSkill.cs`: Boomerang bay lượn nảy qua lại giữa các quái vật.
    2. `EnergyShieldSkill.cs`: Khiên năng lượng bảo vệ người chơi, hấp thụ sát thương.
    3. `GrenadeTossSkill.cs`: Quăng lựu đạn phát nổ diện rộng (AoE).
    4. `OrbitSwordsSkill.cs`: Các lưỡi kiếm xoay quanh bảo vệ người chơi và chém quái lại gần.
    5. `PiercingShotSkill.cs`: Đạn bắn thẳng xuyên qua nhiều mục tiêu.
    6. `SeagullAllySkill.cs`: Triệu hồi chú chim hải âu bay lượn hỗ trợ tấn công quái tự động.
  - **Hệ thống tạo thẻ kỹ năng:**
    - `SkillCardGeneratorEditor.cs`: Công cụ Editor trong Unity hỗ trợ sinh nhanh các ScriptableObject thẻ nâng cấp.
    - Sinh hàng loạt thẻ bài nâng cấp trong thư mục `Assets/UpgradeCards/` (BouncingBoomerang, BurstFire, CoinReward, DamageUp, EnergyShield, GrenadeToss, HealPack, Multishot, OrbitSwords, PiercingShot, SeagullAlly).
  - **Procedural Assets:** Viết `ProceduralSkillAssets.cs` để tạo texture, hiệu ứng hạt và icon kỹ năng bằng code khi chưa có asset đồ họa ngoài.

---

### 📅 Ngày 29/10/2026 (Thứ Năm)
#### 1. Commit `d0e6aac`: *Fix icon*
- **Người thực hiện:** `shikai469`
- **Các tính năng & nội dung hoàn thành:**
  - **Giao diện nâng cấp & Cài đặt:**
    - `UpgradeSceneIconSetup.cs`: Tự động tìm và gán icon tương ứng cho từng thẻ nâng cấp trong màn chọn skill.
    - `SettingsSceneStyler.cs`: Căn chỉnh, bố cục lại toàn bộ thanh trượt Volume, nút bấm và panel cài đặt.
  - **Đường đi bản đồ chọn màn:**
    - `LevelPathRenderer.cs`: Thuật toán vẽ đường nối mềm mại uốn lượn kết nối các nốt màn chơi (Level nodes).
    - Mở rộng `LevelSelectManager.cs` để quản lý việc mở khóa màn và điều hướng node.
  - **Tối ưu hóa Procedural Icons:** Cải tiến thuật toán tạo icon và texture trong `ProceduralSkillAssets.cs` cho sắc nét hơn.

#### 2. Commit `97df9fc`: *Update LevelSelectScene*
- **Người thực hiện:** `shikai469`
- **Các tính năng & nội dung hoàn thành:**
  - **Bản đồ phong cách Candy Crush:**
    - Tích hợp asset bản đồ dọc độ phân giải cao `CandyCrush_Map_900x1500.png` và nút node tròn `CandyCrush_CircleNode.png`.
    - `CandyCrushMapGeneratorEditor.cs`: Công cụ Editor trực quan cho phép tự động bố trí các level node theo đường cong uốn lượn trên bản đồ.
  - **Xem trước màn chơi (Map Preview):**
    - Thêm hình ảnh preview cho 4 chế độ bản đồ: `MapPreview_Easy.png`, `MapPreview_Normal.png`, `MapPreview_Hard.png`, `MapPreview_Endless.png`.
  - **Bộ Icon âm thanh cho UI:**
    - Thêm các sprite icon âm thanh sắc nét: `icon_music_note.png`, `icon_speaker_full.png`, `icon_speaker_half.png`, `icon_speaker_mute.png`.
  - **Tích hợp & cân bằng:**
    - Cập nhật các ScriptableObject thẻ nâng cấp (`FireRateCard`, `SpeedCard`, `MagnetCard`, `BurstFireCard`, `MultishotCard`, `OrbitSwordsCard`).

---

### 📅 Ngày 30/10/2026 (Thứ Sáu)
#### 1. Commit `97f8b47`: *UIRoundedBorder*
- **Người thực hiện:** `shikai469`
- **Các tính năng & nội dung hoàn thành:**
  - **Component UI viền bo tròn tùy biến (`UIRoundedBorder.cs`):**
    - Tự động sinh lưới đồ họa (Mesh UI) bo tròn 4 góc với độ dày viền (border width), bán kính bo (corner radius) và màu sắc tùy chỉnh.
    - Khắc phục triệt để nhược điểm méo hình của ảnh 9-slice thông thường khi co giãn kích thước UI.
  - **Chuẩn hóa giao diện toàn dự án:**
    - Áp dụng `UIRoundedBorder` đồng bộ trên cả 5 cảnh chính:
      - `MainMenu.unity`
      - `LevelSelectScene.unity`
      - `GameplayScene.unity`
      - `SettingsScene.unity`
      - `UpgradeScene.unity`

#### 2. Commit `90abdb6`: *Upgrade Skill Pool*
- **Người thực hiện:** `shikai469`
- **Các tính năng & nội dung hoàn thành:**
  - **Tích hợp bộ Sprite Icon kỹ thuật số chất lượng cao (`Assets/Sprites/UI/Icons/`):**
    - Các icon vũ khí & kỹ năng mới: `Icon_Bat`, `Icon_Boomerang`, `Icon_DamageUp`, `Icon_Grenade`, `Icon_MachineGun`, `Icon_PiercingShot`, `Icon_Seagull`, `Icon_Shotgun`.
    - Asset đồ họa vũ khí và chim hải âu: `weapon.png`, `weaponTrail.png`, `seagull.png`.
  - **Tối ưu & Cân bằng Kỹ năng:**
    - Nâng cấp cơ chế va chạm, vật lý cho Boomerang (`BouncingBoomerangSkill.cs`) và Kiếm xoay (`OrbitSwordsSkill.cs`).
    - Nâng cấp cơ chế phòng thủ cho Khiên (`EnergyShieldSkill.cs`) và ném bom (`GrenadeTossSkill.cs`).
  - **Hoàn thiện UI/Core:**
    - Đồng bộ hóa các icon mới vào các thẻ nâng cấp ScriptableObject và hiển thị trên `UpgradeCardUI`.
    - Nâng cấp `SettingsManager.cs` và `PauseController.cs`: Lưu cấu hình cài đặt âm lượng chuẩn xác khi tạm dừng game.

---

## 4. TỔNG KẾT ĐÁNH GIÁ

1. **Hiệu suất công việc:** Khối lượng công việc hoàn thành rất lớn, bao quát từ hệ thống cốt lõi (Core/Combat/Skills), giao diện người dùng (UI/UX) đến các công cụ Editor tự động hóa giúp tăng tốc quy trình làm game.
2. **Chất lượng mã nguồn:**
   - Cấu trúc thư mục gọn gàng, phân tách rõ ràng trách nhiệm từng module.
   - Các tính năng kỹ năng được module hóa thành từng component độc lập, dễ dàng gắn vào người chơi hoặc quái vật.
   - Giao diện người dùng được nâng cấp chuyên nghiệp nhờ `UIRoundedBorder` và bộ icon đồng bộ.
3. **Đề xuất cho giai đoạn tiếp theo:**
   - Chạy thử nghiệm playtest để kiểm tra cân bằng chỉ số (sát thương, thời gian hồi chiêu) của 6 kỹ năng mới.
   - Kiểm tra hiệu năng (Profiling) trên thiết bị di động/máy yếu đối với các hiệu ứng hạt và số lượng quái đông.
   - Hoàn tất lưu dữ liệu tiến trình hoàn thành các màn chơi (Save/Load Level Progress) từ Level Select Scene.
