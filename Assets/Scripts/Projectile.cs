using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class Projectile : MonoBehaviour
{
    ProjectileSpawner owner;
    Rigidbody body;
    float knockbackForce = 5f;

    public Rigidbody Body => body;

    public void Initialize(ProjectileSpawner spawner, Rigidbody attachedBody)
    {
        owner = spawner;
        body = attachedBody;
    }

    public void SetKnockback(float force)
    {
        knockbackForce = Mathf.Max(0f, force);
    }

    void Awake()
    {
        if (body == null)
        {
            body = GetComponent<Rigidbody>();
        }

        if (body != null)
        {
            body.useGravity = false;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.interpolation = RigidbodyInterpolation.Interpolate;
        }

        var sphereCollider = GetComponent<Collider>();
        if (sphereCollider != null)
        {
            sphereCollider.material = null;
        }
    }

    void OnEnable()
    {
        if (body != null)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision == null)
        {
            owner?.RecycleProjectile(this);
            return;
        }

        var player = collision.collider != null
            ? collision.collider.GetComponent<PlayerController>() ?? collision.collider.GetComponentInParent<PlayerController>()
            : null;

        if (player != null)
        {
            var playerBody = player.GetComponent<Rigidbody>();
            if (playerBody != null)
            {
                Vector3 pushDirection = (player.transform.position - transform.position).normalized;
                pushDirection.y = Mathf.Clamp(pushDirection.y + 0.35f, -0.1f, 0.9f);
                playerBody.AddForce(pushDirection * knockbackForce, ForceMode.VelocityChange);
            }
        }

        owner?.RecycleProjectile(this);
    }
}
