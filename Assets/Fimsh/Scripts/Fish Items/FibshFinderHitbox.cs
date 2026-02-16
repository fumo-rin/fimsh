using RinCore;
using UnityEngine;
[RequireComponent(typeof(BoxCollider))]
public class FibshFinderHitbox : MonoBehaviour
{
    [SerializeField] FishPlayer player;
    [SerializeField] ACWrapper catchSound;
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<IFibsh>(out IFibsh f) && other.gameObject != null && other.gameObject.activeInHierarchy && f.TryCollect(player))
        {
            catchSound.Play(transform.position);
            Destroy(other.gameObject);
        }
    }
}
