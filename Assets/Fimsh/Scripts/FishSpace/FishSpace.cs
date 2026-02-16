using RinCore;
using UnityEngine;

public class FishSpace : MonoBehaviour
{
    [SerializeField] Transform start0, start1, end0, end1;
    static FishSpace instance;
    private void Awake()
    {
        instance = this;
    }
    public static void Map(float lerp, float x01, out Vector3 map)
    {
        map = new();
        if (instance is FishSpace f && f != null && f.gameObject != null)
        {
            Vector3 startX = f.start0.position.LerpUnclamped(f.start1.position, x01);
            Vector3 endX = f.end0.position.LerpUnclamped(f.end1.position, x01);
            map = startX.LerpUnclamped(endX, lerp);
        }
    }
}
