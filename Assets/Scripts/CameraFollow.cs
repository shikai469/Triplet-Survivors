using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform player; // Kéo thả object Player vào đây
    public Vector3 offset = new Vector3(0, 0, -10f); // Giữ khoảng cách trục Z để camera không bị kẹt vào nhân vật

    // Sử dụng LateUpdate thay vì Update để đảm bảo Camera chỉ di chuyển SAU KHI nhân vật đã tính toán xong vật lý
    void LateUpdate()
    {
        if (player != null)
        {
            transform.position = player.position + offset;
        }
    }
}