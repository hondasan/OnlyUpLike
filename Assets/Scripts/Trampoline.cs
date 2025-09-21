using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Trampoline : MonoBehaviour
{
    public float bounce = 12f;
    public float cooldown = 0.25f;
    public Vector3 bounceDirection = Vector3.up;

    readonly Dictionary<Rigidbody, float> nextBounceTimes = new Dictionary<Rigidbody, float>();
    CameraShake cachedShake;

    void Reset()
    {
        var col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = false;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        HandleCollision(collision.collider, collision.rigidbody);
    }

    void OnTriggerEnter(Collider other)
    {
        HandleCollision(other, other.attachedRigidbody);
    }

    void HandleCollision(Collider other, Rigidbody body)
    {
        if (other == null)
        {
            return;
        }

        var player = other.GetComponent<PlayerController>() ?? other.GetComponentInParent<PlayerController>();
        if (player == null)
        {
            return;
        }

        if (body == null)
        {
            body = player.GetComponent<Rigidbody>();
        }

        if (body == null)
        {
            return;
        }

        float now = Time.time;
        if (nextBounceTimes.TryGetValue(body, out float nextAllowed) && nextAllowed > now)
        {
            return;
        }

        Vector3 dir = bounceDirection.sqrMagnitude > 0.0001f ? bounceDirection.normalized : Vector3.up;
        Vector3 velocity = body.linearVelocity;
        float along = Vector3.Dot(velocity, dir);
        if (along < 0f)
        {
            velocity -= dir * along;
            body.linearVelocity = velocity;
        }

        body.AddForce(dir * bounce, ForceMode.VelocityChange);
        nextBounceTimes[body] = now + Mathf.Max(0f, cooldown);

        var sfx = SFXManager.Instance;
        if (sfx != null)
        {
            sfx.PlayTrampoline(transform.position);
        }

        if (cachedShake == null)
        {
            cachedShake = FindObjectOfType<CameraShake>();
        }

        if (cachedShake != null)
        {
            cachedShake.ShakeOnce(0.25f, 0.1f);
        }
    }
}
