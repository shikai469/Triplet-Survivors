using UnityEngine;

public class InfiniteBackground : MonoBehaviour
{
    private Material mat;

    // Đặt là 1 để nền trôi khớp với bước chân, đặt 0.5 để nền trôi chậm hơn tạo cảm giác ở xa
    public float scrollFactor = 1f;

    void Start()
    {
        mat = GetComponent<MeshRenderer>().material;
    }

    void Update()
    {
        // Tự động chia cho Scale 20x20 để tốc độ trôi hoàn toàn chuẩn xác
        float x = (transform.position.x / transform.localScale.x) * scrollFactor;
        float y = (transform.position.y / transform.localScale.y) * scrollFactor;

        Vector2 offset = new Vector2(x, y);

        // Lệnh bao quát ép trượt ảnh cho cả Shader 2D và 3D URP
        mat.mainTextureOffset = offset;
        if (mat.HasProperty("_BaseMap")) mat.SetTextureOffset("_BaseMap", offset);
        if (mat.HasProperty("_MainTex")) mat.SetTextureOffset("_MainTex", offset);
    }
}