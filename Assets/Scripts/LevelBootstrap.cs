using System.Collections.Generic;
using UnityEngine;

public class LevelBootstrap : MonoBehaviour
{
    [Header("Generation")]
    public int totalSteps = 60;
    public float baseStepHeight = 1.6f;
    public float heightVariance = 0.35f;
    public float stepDistance = 5f;
    public float angleStepDegrees = 32f;
    public Vector2 platformSizeRange = new Vector2(2.8f, 4.5f);
    public float platformThickness = 0.6f;
    public float horizontalJitter = 1.2f;
    public Vector3 startPlatformSize = new Vector3(6f, 0.6f, 6f);
    public int seed = 1234;
    public string generatedRootName = "GeneratedCourseRoot";
    public bool rebuildOnStart = true;

    [Header("Difficulty Curve")]
    [Tooltip("Number of early steps that should be wide and easy to land on.")]
    public int easyStepCount = 10;
    public Vector2 easyPlatformSizeRange = new Vector2(5.5f, 7.5f);
    public float easyStepDistance = 3.2f;
    public float easyHorizontalJitter = 0.5f;
    public float easyHeightVariance = 0.15f;
    [Tooltip("Step index from which moving/rotating/breakable gimmicks may appear.")]
    public int gimmickStartStep = 6;
    [Tooltip("Step index that marks the start of the hardest platforms.")]
    public int lateDifficultyStart = 32;
    public Vector2 latePlatformSizeRange = new Vector2(2.2f, 3.4f);
    public float farStepDistance = 8.5f;
    public float farHorizontalJitter = 2.2f;
    public float farHeightVarianceMultiplier = 1.6f;
    public float spiralRadiusStart = 4f;
    public float spiralRadiusEnd = 30f;

    [Header("Checkpoints")]
    public int checkpointEvery = 10;
    public float checkpointHeightOffset = 1.3f;

    [Header("Gimmick Probability")]
    [Range(0f, 1f)] public float movingChance = 0.12f;
    [Range(0f, 1f)] public float rotatingChance = 0.2f;
    [Range(0f, 1f)] public float breakableChance = 0.18f;
    [Range(0f, 1f)] public float trampolineChance = 0.12f;
    [Range(0f, 1f)] public float windChance = 0.1f;
    [Range(0f, 1f)] public float icyChance = 0.15f;

    [Header("Wind Settings")]
    public Vector3 windZoneSize = new Vector3(4f, 3f, 4f);

    [Header("Projectile Spawner")]
    public bool enableProjectileSpawner = true;
    public float projectileSpawnHeightOffset = 6f;
    public float projectileSpawnDistanceOffset = 8f;
    public int projectilePoolSize = 8;
    public float projectileFireInterval = 4f;
    public float projectileInitialDelay = 1.5f;
    public float projectileSpeed = 14f;
    public float projectileLifetime = 8f;
    public float projectileScale = 1.1f;
    public float projectileAimVariance = 1.4f;
    public float projectileKnockbackForce = 6f;

    System.Random rng;

    void Start()
    {
        if (rebuildOnStart)
        {
            BuildCourse();
        }
        else
        {
            PrepareRoot();
        }
    }

    public void BuildCourse()
    {
        Transform root = PrepareRoot();
        if (root == null)
        {
            return;
        }

        var player = EnsurePlayer();
        EnsureHud();
        EnsureSfxManager();

        if (totalSteps < 1)
        {
            totalSteps = 1;
        }

        rng = CreateRandom(seed);

        var platformPositions = new List<Vector3>();

        GameObject startPlatform = CreatePlatform(root, Vector3.zero, startPlatformSize, 0, "StartPlatform");
        platformPositions.Add(startPlatform.transform.position);

        if (player != null)
        {
            if (player.GetComponent<PlatformRider>() == null)
            {
                player.gameObject.AddComponent<PlatformRider>();
            }

            if (player.respawnPoint == null)
            {
                player.respawnPoint = startPlatform.transform;
            }
        }

        GameObject previousPlatform = startPlatform;

        for (int i = 1; i < totalSteps; i++)
        {
            float angle = Mathf.Deg2Rad * angleStepDegrees * i;
            Vector3 radial = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            float totalStepsMinusOne = Mathf.Max(1, totalSteps - 1);
            float normalizedIndex = i / totalStepsMinusOne;
            float difficultyProgress = Mathf.Clamp01((i - easyStepCount) / Mathf.Max(1, totalSteps - easyStepCount));
            float difficultyEase = Mathf.SmoothStep(0f, 1f, difficultyProgress);

            float radius = Mathf.Lerp(spiralRadiusStart, spiralRadiusEnd, Mathf.SmoothStep(0f, 1f, normalizedIndex));
            float distanceTarget = Mathf.Lerp(stepDistance, farStepDistance, difficultyEase);
            float currentStepDistance = Mathf.Lerp(easyStepDistance, distanceTarget, difficultyEase);

            Vector3 previousPosition = previousPlatform != null ? previousPlatform.transform.position : Vector3.zero;
            Vector3 alongPath = previousPosition + radial * currentStepDistance;
            Vector3 outward = radial * radius;
            Vector3 basePosition = Vector3.Lerp(alongPath, outward, Mathf.SmoothStep(0f, 1f, normalizedIndex));

            float jitterTarget = Mathf.Lerp(horizontalJitter, farHorizontalJitter, difficultyEase);
            float currentHorizontalJitter = Mathf.Lerp(easyHorizontalJitter, jitterTarget, difficultyEase);
            Vector3 jitter = new Vector3(RandomRange(-currentHorizontalJitter, currentHorizontalJitter), 0f, RandomRange(-currentHorizontalJitter, currentHorizontalJitter));

            float heightVarianceTarget = heightVariance * Mathf.Lerp(1f, farHeightVarianceMultiplier, difficultyEase);
            float currentHeightVariance = Mathf.Lerp(easyHeightVariance, heightVarianceTarget, difficultyEase);
            float height = baseStepHeight * i + RandomRange(-currentHeightVariance, currentHeightVariance);

            Vector3 position = basePosition + jitter;
            position.y = height;

            Vector2 sizeRange = easyPlatformSizeRange;
            if (i >= lateDifficultyStart)
            {
                sizeRange = latePlatformSizeRange;
            }
            else if (i >= easyStepCount)
            {
                sizeRange = platformSizeRange;
            }

            float sizeX = RandomRange(sizeRange.x, sizeRange.y);
            float sizeZ = RandomRange(sizeRange.x, sizeRange.y);
            Vector3 scale = new Vector3(sizeX, platformThickness, sizeZ);

            GameObject platform = CreatePlatform(root, position, scale, i, $"Platform_{i:000}");
            DecoratePlatform(platform, root, i);
            platformPositions.Add(position);

            if (checkpointEvery > 0 && i % checkpointEvery == 0)
            {
                CreateCheckpoint(root, platform.transform.position + Vector3.up * (checkpointHeightOffset + platform.transform.localScale.y * 0.5f), i);
            }

            previousPlatform = platform;
        }

        if (enableProjectileSpawner)
        {
            CreateProjectileSpawner(root, platformPositions, player != null ? player.transform : null);
        }

        if (previousPlatform != null)
        {
            CreateGoal(previousPlatform.transform.position + Vector3.up * 3.5f, root);
        }
    }

    Transform PrepareRoot()
    {
        GameObject rootObj = GameObject.Find(generatedRootName);
        if (rootObj == null)
        {
            rootObj = new GameObject(generatedRootName);
        }

        rootObj.transform.SetParent(transform, false);
        ClearChildren(rootObj.transform);
        return rootObj.transform;
    }

    void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            var child = parent.GetChild(i);
            if (Application.isPlaying)
            {
                Destroy(child.gameObject);
            }
#if UNITY_EDITOR
            else
            {
                DestroyImmediate(child.gameObject);
            }
#endif
        }
    }

    System.Random CreateRandom(int seedValue)
    {
        return seedValue == 0 ? new System.Random() : new System.Random(seedValue);
    }

    float RandomRange(float min, float max)
    {
        if (rng == null)
        {
            rng = CreateRandom(seed);
        }

        return Mathf.Lerp(min, max, (float)rng.NextDouble());
    }

    GameObject CreatePlatform(Transform root, Vector3 position, Vector3 scale, int index, string name)
    {
        GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
        platform.name = name;
        platform.transform.SetParent(root, false);
        platform.transform.position = position;
        platform.transform.localScale = scale;

        var renderer = platform.GetComponent<Renderer>();
        if (renderer != null)
        {
            Color baseColor = Color.Lerp(new Color(0.4f, 0.7f, 1f), new Color(1f, 0.8f, 0.6f), Mathf.Repeat(index * 0.21f, 1f));
            renderer.material.color = baseColor;
        }

        return platform;
    }

    void DecoratePlatform(GameObject platform, Transform root, int index)
    {
        if (platform == null)
        {
            return;
        }

        if (index < gimmickStartStep)
        {
            return;
        }

        bool addedMoving = false;
        if (RandomRange(0f, 1f) < movingChance)
        {
            addedMoving = SetupMovingPlatform(platform, root);
        }

        if (!addedMoving && RandomRange(0f, 1f) < rotatingChance)
        {
            var rotating = platform.AddComponent<RotatingPlatform>();
            rotating.degreesPerSecond = RandomRange(35f, 90f) * (RandomRange(0f, 1f) > 0.5f ? 1f : -1f);
            rotating.axis = Vector3.up;
        }

        bool addedBreakable = false;
        if (!addedMoving && RandomRange(0f, 1f) < breakableChance)
        {
            var breakable = platform.AddComponent<BreakablePlatform>();
            breakable.delay = RandomRange(0.4f, 1.1f);
            breakable.cooldown = RandomRange(2.5f, 4.5f);
            TintPlatform(platform, new Color(1f, 0.65f, 0.5f));
            addedBreakable = true;
        }

        if (!addedBreakable && RandomRange(0f, 1f) < trampolineChance)
        {
            var trampoline = platform.AddComponent<Trampoline>();
            trampoline.bounce = RandomRange(11f, 15f);
            trampoline.cooldown = RandomRange(0.2f, 0.4f);
            TintPlatform(platform, new Color(0.5f, 1f, 0.5f));
        }

        if (RandomRange(0f, 1f) < windChance)
        {
            CreateWindVolume(root, platform.transform.position, index);
        }

        if (RandomRange(0f, 1f) < icyChance)
        {
            EnsurePhysicsMaterials.ApplyIce(platform.GetComponent<Collider>());
            TintPlatform(platform, new Color(0.7f, 0.9f, 1f));
        }
    }

    bool SetupMovingPlatform(GameObject platform, Transform root)
    {
        if (platform == null || root == null)
        {
            return false;
        }

        var mover = platform.AddComponent<MovingPlatform>();
        mover.speed = RandomRange(1.5f, 3.5f);
        mover.startAtPointA = RandomRange(0f, 1f) > 0.5f;

        Vector3 travelDir = new Vector3(RandomRange(-1f, 1f), 0f, RandomRange(-1f, 1f));
        if (travelDir.sqrMagnitude < 0.001f)
        {
            travelDir = Vector3.right;
        }
        travelDir.Normalize();
        float travelDistance = RandomRange(2f, 4f);
        Vector3 travel = travelDir * travelDistance;

        Vector3 pointA = platform.transform.position - travel * 0.5f;
        Vector3 pointB = platform.transform.position + travel * 0.5f;

        mover.pointA = CreateAnchor(root, platform.name + "_PointA", pointA);
        mover.pointB = CreateAnchor(root, platform.name + "_PointB", pointB);
        return true;
    }

    Transform CreateAnchor(Transform root, string name, Vector3 position)
    {
        var anchorObj = new GameObject(name);
        anchorObj.transform.SetParent(root, false);
        anchorObj.transform.position = position;
        return anchorObj.transform;
    }

    void TintPlatform(GameObject platform, Color color)
    {
        if (platform == null)
        {
            return;
        }

        var renderer = platform.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = color;
        }
    }

    void CreateWindVolume(Transform root, Vector3 position, int index)
    {
        var windObj = new GameObject($"WindZone_{index:000}");
        windObj.transform.SetParent(root, false);
        windObj.transform.position = position + Vector3.up * (windZoneSize.y * 0.5f + 0.5f);

        var box = windObj.AddComponent<BoxCollider>();
        box.size = windZoneSize;
        box.isTrigger = true;

        var wind = windObj.AddComponent<WindZoneVolume>();
        wind.forceDirection = new Vector3(RandomRange(-0.4f, 0.4f), RandomRange(0.2f, 0.8f), RandomRange(-0.4f, 0.4f));
        wind.strength = RandomRange(3f, 7f);
    }

    void CreateCheckpoint(Transform root, Vector3 position, int index)
    {
        GameObject checkpointRoot = new GameObject($"Checkpoint_{index:000}");
        checkpointRoot.transform.SetParent(root, false);
        checkpointRoot.transform.position = position;

        var trigger = checkpointRoot.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = 0.7f;
        checkpointRoot.AddComponent<Checkpoint>();

        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        marker.name = "Marker";
        marker.transform.SetParent(checkpointRoot.transform, false);
        marker.transform.localScale = new Vector3(0.35f, 1.4f, 0.35f);
        var renderer = marker.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = new Color(0.9f, 0.8f, 0.3f);
        }

        var markerCollider = marker.GetComponent<Collider>();
        if (markerCollider != null)
        {
            if (Application.isPlaying)
            {
                Destroy(markerCollider);
            }
#if UNITY_EDITOR
            else
            {
                DestroyImmediate(markerCollider);
            }
#endif
        }
    }

    void CreateGoal(Vector3 position, Transform root)
    {
        GameObject goal = GameObject.CreatePrimitive(PrimitiveType.Cube);
        goal.name = "Goal";
        goal.transform.SetParent(root, false);
        goal.transform.position = position;
        goal.transform.localScale = new Vector3(2f, 2f, 2f);
        var renderer = goal.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = new Color(1f, 0.6f, 0.2f);
        }
        var collider = goal.GetComponent<Collider>();
        if (collider != null)
        {
            collider.isTrigger = true;
        }
    }

    PlayerController EnsurePlayer()
    {
        return FindObjectOfType<PlayerController>();
    }

    void EnsureHud()
    {
        if (FindObjectOfType<GameHUD>() == null)
        {
            var hudObject = new GameObject("GameHUD");
            hudObject.AddComponent<GameHUD>();
        }
    }

    void EnsureSfxManager()
    {
        if (FindObjectOfType<SFXManager>() == null)
        {
            var sfxObject = new GameObject("SFXManager");
            sfxObject.AddComponent<AudioSource>();
            sfxObject.AddComponent<SFXManager>();
        }
    }

    void CreateProjectileSpawner(Transform root, List<Vector3> positions, Transform target)
    {
        if (!enableProjectileSpawner || root == null || positions == null || positions.Count < 2)
        {
            return;
        }

        int index = Mathf.Clamp(Mathf.RoundToInt(positions.Count * 0.55f), 1, positions.Count - 1);
        Vector3 anchor = positions[index];
        Vector3 lookTarget = positions[Mathf.Min(index + 2, positions.Count - 1)];

        Vector3 outward = anchor - Vector3.zero;
        if (outward.sqrMagnitude < 0.01f)
        {
            outward = Vector3.forward;
        }

        Vector3 spawnPosition = anchor + outward.normalized * projectileSpawnDistanceOffset + Vector3.up * projectileSpawnHeightOffset;

        var spawnerObject = new GameObject("ProjectileSpawner");
        spawnerObject.transform.SetParent(root, false);
        spawnerObject.transform.position = spawnPosition;
        spawnerObject.transform.forward = (lookTarget - spawnPosition).sqrMagnitude > 0.01f ? (lookTarget - spawnPosition).normalized : outward.normalized;

        var spawner = spawnerObject.AddComponent<ProjectileSpawner>();
        spawner.poolSize = projectilePoolSize;
        spawner.fireInterval = projectileFireInterval;
        spawner.initialDelay = projectileInitialDelay;
        spawner.projectileSpeed = projectileSpeed;
        spawner.projectileLifetime = projectileLifetime;
        spawner.projectileScale = projectileScale;
        spawner.aimVariance = projectileAimVariance;
        spawner.knockbackForce = projectileKnockbackForce;
        spawner.SetTarget(target);
        spawner.ResetTimer();
    }
}
