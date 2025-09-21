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

        GameObject startPlatform = CreatePlatform(root, Vector3.zero, startPlatformSize, 0, "StartPlatform");

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
            Vector3 horizontal = radial * stepDistance;
            Vector3 jitter = new Vector3(RandomRange(-horizontalJitter, horizontalJitter), 0f, RandomRange(-horizontalJitter, horizontalJitter));

            float height = baseStepHeight * i + RandomRange(-heightVariance, heightVariance);
            Vector3 position = horizontal + jitter;
            position.y = height;

            float sizeX = RandomRange(platformSizeRange.x, platformSizeRange.y);
            float sizeZ = RandomRange(platformSizeRange.x, platformSizeRange.y);
            Vector3 scale = new Vector3(sizeX, platformThickness, sizeZ);

            GameObject platform = CreatePlatform(root, position, scale, i, $"Platform_{i:000}");
            DecoratePlatform(platform, root, i);

            if (checkpointEvery > 0 && i % checkpointEvery == 0)
            {
                CreateCheckpoint(root, platform.transform.position + Vector3.up * (checkpointHeightOffset + platform.transform.localScale.y * 0.5f), i);
            }

            previousPlatform = platform;
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
}
