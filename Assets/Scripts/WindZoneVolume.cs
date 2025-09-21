using UnityEngine;

[RequireComponent(typeof(Collider))]
public class WindZoneVolume : MonoBehaviour
{
    public Vector3 forceDirection = new Vector3(1f, 0f, 0f);
    public float strength = 5f;
    public ForceMode forceMode = ForceMode.Acceleration;

    Collider zoneCollider;

    void Reset()
    {
        EnsureTrigger();
    }

    void Awake()
    {
        zoneCollider = GetComponent<Collider>();
        EnsureTrigger();
    }

    void EnsureTrigger()
    {
        if (zoneCollider == null)
        {
            zoneCollider = GetComponent<Collider>();
        }

        if (zoneCollider != null)
        {
            zoneCollider.isTrigger = true;
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (Mathf.Approximately(strength, 0f) || other == null)
        {
            return;
        }

        var body = other.attachedRigidbody;
        if (body == null)
        {
            return;
        }

        Vector3 dir = forceDirection.sqrMagnitude > 0.0001f ? forceDirection.normalized : Vector3.forward;
        Vector3 worldForce = transform.TransformDirection(dir) * Mathf.Abs(strength);
        body.AddForce(worldForce, forceMode);
    }

    void OnDrawGizmosSelected()
    {
        var col = zoneCollider != null ? zoneCollider : GetComponent<Collider>();
        if (col == null)
        {
            return;
        }

        Gizmos.color = new Color(0.6f, 0.8f, 1f, 0.4f);
        Vector3 center = col.bounds.center;
        Gizmos.DrawWireCube(center, col.bounds.size);

        Vector3 dir = forceDirection.sqrMagnitude > 0.0001f ? forceDirection.normalized : Vector3.forward;
        Gizmos.DrawRay(center, transform.TransformDirection(dir) * 2f);
    }
}
