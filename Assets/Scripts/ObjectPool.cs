using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Component đánh dấu đối tượng được sinh ra từ Object Pool.
/// Tự lưu tham chiếu đến Prefab gốc và trạng thái trong pool để xử lý O(1) không cần GetInstanceID.
/// </summary>
[DisallowMultipleComponent]
public class PooledObjectInstance : MonoBehaviour
{
    public GameObject originPrefab;
    public bool isAvailableInPool;
}

/// <summary>
/// Hệ thống Object Pool chung cho toàn bộ game.
/// Tái sử dụng GameObject thay vì Instantiate/Destroy liên tục để tránh GC lag trên mobile.
/// Tự động khởi tạo nếu scene chưa gắn sẵn ObjectPool.
/// </summary>
public class ObjectPool : MonoBehaviour
{
    private static ObjectPool _instance;

    public static ObjectPool Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<ObjectPool>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("[ObjectPool]");
                    _instance = go.AddComponent<ObjectPool>();
                }
            }
            return _instance;
        }
    }

    // Cấu hình pre-warm cho từng loại prefab
    [System.Serializable]
    public class PoolConfig
    {
        public GameObject prefab;
        public int prewarmCount = 10;
    }

    [Header("Pre-warm Configs (Tùy chọn)")]
    public List<PoolConfig> prewarmConfigs = new List<PoolConfig>();

    // Pool lưu trữ: Key = Prefab gốc
    private readonly Dictionary<GameObject, Queue<GameObject>> poolDict = new Dictionary<GameObject, Queue<GameObject>>();

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // Pre-warm các pool theo cấu hình trên Inspector
        if (prewarmConfigs != null)
        {
            foreach (var config in prewarmConfigs)
            {
                if (config.prefab != null)
                {
                    Prewarm(config.prefab, config.prewarmCount);
                }
            }
        }
    }

    /// <summary>
    /// Tạo sẵn một số lượng object cho pool để tránh lag đột biến khi trận mới bắt đầu.
    /// </summary>
    public void Prewarm(GameObject prefab, int count)
    {
        if (prefab == null) return;

        EnsurePoolExists(prefab);

        for (int i = 0; i < count; i++)
        {
            GameObject obj = Instantiate(prefab, transform);
            obj.SetActive(false);

            PooledObjectInstance marker = obj.GetComponent<PooledObjectInstance>();
            if (marker == null) marker = obj.AddComponent<PooledObjectInstance>();
            marker.originPrefab = prefab;
            marker.isAvailableInPool = true;

            poolDict[prefab].Enqueue(obj);
        }
    }

    /// <summary>
    /// Lấy một object từ pool (thay thế Instantiate).
    /// </summary>
    public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null) return null;

        EnsurePoolExists(prefab);

        Queue<GameObject> pool = poolDict[prefab];

        // Lấy từ pool nếu có sẵn
        while (pool.Count > 0)
        {
            GameObject obj = pool.Dequeue();
            if (obj != null)
            {
                PooledObjectInstance marker = obj.GetComponent<PooledObjectInstance>();
                if (marker != null)
                {
                    marker.isAvailableInPool = false;
                }

                obj.transform.SetParent(null);
                obj.transform.SetPositionAndRotation(position, rotation);
                obj.SetActive(true);
                return obj;
            }
            // Object bị null (scene unload) -> bỏ qua, lấy cái tiếp theo
        }

        // Pool hết -> tạo mới và gắn marker
        GameObject newObj = Instantiate(prefab, position, rotation);
        PooledObjectInstance newMarker = newObj.GetComponent<PooledObjectInstance>();
        if (newMarker == null) newMarker = newObj.AddComponent<PooledObjectInstance>();
        newMarker.originPrefab = prefab;
        newMarker.isAvailableInPool = false;

        return newObj;
    }

    /// <summary>
    /// Lấy Component từ pool theo kiểu generic.
    /// </summary>
    public T Get<T>(T prefab, Vector3 position, Quaternion rotation) where T : Component
    {
        if (prefab == null) return null;
        GameObject obj = Get(prefab.gameObject, position, rotation);
        return obj != null ? obj.GetComponent<T>() : null;
    }

    /// <summary>
    /// Trả object về pool (thay thế Destroy).
    /// </summary>
    public void Return(GameObject obj)
    {
        if (obj == null) return;

        PooledObjectInstance marker = obj.GetComponent<PooledObjectInstance>();
        if (marker != null && marker.originPrefab != null)
        {
            // Nếu đã nằm trong pool rồi -> tránh double return
            if (marker.isAvailableInPool) return;

            marker.isAvailableInPool = true;
            obj.SetActive(false);
            obj.transform.SetParent(transform);

            if (!poolDict.ContainsKey(marker.originPrefab))
            {
                poolDict[marker.originPrefab] = new Queue<GameObject>();
            }

            poolDict[marker.originPrefab].Enqueue(obj);
        }
        else
        {
            // Object không thuộc pool -> Destroy an toàn nếu chưa tắt
            if (obj.activeSelf)
            {
                Destroy(obj);
            }
        }
    }

    /// <summary>
    /// Trả object về pool sau một khoảng thời gian (thay thế Destroy(obj, delay)).
    /// </summary>
    public void Return(GameObject obj, float delay)
    {
        if (obj == null) return;
        StartCoroutine(ReturnDelayedRoutine(obj, delay));
    }

    private IEnumerator ReturnDelayedRoutine(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (obj != null && obj.activeSelf)
        {
            Return(obj);
        }
    }

    private void EnsurePoolExists(GameObject prefab)
    {
        if (!poolDict.ContainsKey(prefab))
        {
            poolDict[prefab] = new Queue<GameObject>();
        }
    }

    void OnDestroy()
    {
        poolDict.Clear();
        if (_instance == this)
        {
            _instance = null;
        }
    }
}
