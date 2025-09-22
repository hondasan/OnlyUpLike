using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileSpawner : MonoBehaviour
{
    [Header("Timing")]
    public float fireInterval = 4f;
    public float initialDelay = 1.5f;

    [Header("Projectile")]
    public int poolSize = 8;
    public float projectileSpeed = 12f;
    public float projectileLifetime = 6f;
    public float projectileScale = 1f;
    public float aimVariance = 1.2f;
    public float knockbackForce = 6f;

    readonly Queue<Projectile> availableProjectiles = new Queue<Projectile>();
    readonly Dictionary<Projectile, Coroutine> lifeRoutines = new Dictionary<Projectile, Coroutine>();

    Transform target;
    float fireTimer;
    bool poolInitialized;

    void OnEnable()
    {
        ResetTimer();
    }

    void Start()
    {
        EnsurePool();
    }

    void Update()
    {
        if (!poolInitialized)
        {
            EnsurePool();
        }

        if (target == null)
        {
            var player = FindObjectOfType<PlayerController>();
            if (player != null)
            {
                target = player.transform;
            }
        }

        if (target == null || poolSize <= 0)
        {
            return;
        }

        fireTimer -= Time.deltaTime;
        if (fireTimer > 0f)
        {
            return;
        }

        fireTimer = fireInterval;
        FireProjectile();
    }

    void EnsurePool()
    {
        if (poolInitialized)
        {
            return;
        }

        poolInitialized = true;
        availableProjectiles.Clear();

        int count = Mathf.Max(1, poolSize);
        for (int i = 0; i < count; i++)
        {
            availableProjectiles.Enqueue(CreateProjectile());
        }
    }

    Projectile CreateProjectile()
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "Projectile";
        sphere.transform.SetParent(transform, false);
        sphere.transform.localScale = Vector3.one * Mathf.Max(0.2f, projectileScale);

        var rb = sphere.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        var projectile = sphere.AddComponent<Projectile>();
        projectile.Initialize(this, rb);

        sphere.SetActive(false);
        return projectile;
    }

    void FireProjectile()
    {
        if (target == null)
        {
            return;
        }

        if (availableProjectiles.Count == 0)
        {
            availableProjectiles.Enqueue(CreateProjectile());
        }

        Projectile projectile = availableProjectiles.Dequeue();
        projectile.SetKnockback(knockbackForce);

        Transform projTransform = projectile.transform;
        projTransform.position = transform.position;
        projTransform.rotation = Quaternion.LookRotation((target.position - transform.position).normalized, Vector3.up);

        projectile.gameObject.SetActive(true);

        var rb = projectile.Body;
        if (rb != null)
        {
            Vector3 aimPoint = target.position;
            if (aimVariance > 0f)
            {
                Vector3 randomOffset = Random.insideUnitSphere * aimVariance;
                randomOffset.y *= 0.5f;
                aimPoint += randomOffset;
            }

            Vector3 direction = (aimPoint - transform.position).normalized;
            rb.linearVelocity = direction * projectileSpeed;
            rb.angularVelocity = Vector3.zero;
        }

        Coroutine lifeRoutine = StartCoroutine(HandleLifetime(projectile, projectileLifetime));
        lifeRoutines[projectile] = lifeRoutine;
    }

    IEnumerator HandleLifetime(Projectile projectile, float lifetime)
    {
        yield return new WaitForSeconds(lifetime);

        if (projectile != null && projectile.gameObject.activeSelf)
        {
            RecycleProjectile(projectile);
        }
    }

    public void RecycleProjectile(Projectile projectile)
    {
        if (projectile == null)
        {
            return;
        }

        if (lifeRoutines.TryGetValue(projectile, out Coroutine routine))
        {
            StopCoroutine(routine);
            lifeRoutines.Remove(projectile);
        }

        var rb = projectile.Body;
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        projectile.gameObject.SetActive(false);
        projectile.transform.SetParent(transform, false);

        availableProjectiles.Enqueue(projectile);
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    public void ResetTimer()
    {
        fireTimer = Mathf.Max(0f, initialDelay);
    }
}
