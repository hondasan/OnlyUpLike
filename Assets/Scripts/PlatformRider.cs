using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlatformRider : MonoBehaviour
{
    Transform defaultParent;
    MovingPlatform currentPlatform;

    void Awake()
    {
        defaultParent = transform.parent;
    }

    void OnCollisionEnter(Collision collision)
    {
        TryAttach(collision.transform);
    }

    void OnCollisionStay(Collision collision)
    {
        TryAttach(collision.transform);
    }

    void OnCollisionExit(Collision collision)
    {
        if (currentPlatform == null)
        {
            return;
        }

        var platform = collision.transform.GetComponentInParent<MovingPlatform>();
        if (platform == currentPlatform)
        {
            Detach();
        }
    }

    void OnDisable()
    {
        Detach();
    }

    void TryAttach(Transform other)
    {
        var platform = other.GetComponentInParent<MovingPlatform>();
        if (platform == null)
        {
            return;
        }

        if (currentPlatform == platform)
        {
            return;
        }

        currentPlatform = platform;
        transform.SetParent(platform.transform, true);
    }

    void Detach()
    {
        if (currentPlatform != null)
        {
            currentPlatform = null;
            transform.SetParent(defaultParent, true);
        }
    }
}
