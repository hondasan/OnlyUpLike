using System.Collections.Generic;
using UnityEngine;

public class LevelBootstrap : MonoBehaviour
{
    [Header("Course Layout")]
    [SerializeField] int minPlatforms = 8;
    [SerializeField] int maxPlatforms = 10;
    [SerializeField] Vector2 heightStepRange = new Vector2(1.1f, 1.9f);
    [SerializeField] Vector2 forwardStepRange = new Vector2(2.5f, 3.8f);
    [SerializeField] Vector2 platformSizeRange = new Vector2(2.5f, 4.2f);
    [SerializeField] float platformThickness = 0.6f;
    [SerializeField] float lateralSpread = 2.2f;
    [SerializeField] string generatedRootName = "GeneratedCourseRoot";

    void Start()
    {
        Transform root = PrepareRoot();
        var player = FindObjectOfType<PlayerController>();

        if (player != null && player.GetComponent<PlatformRider>() == null)
        {
            player.gameObject.AddComponent<PlatformRider>();
        }

        if (FindObjectOfType<GameHUD>() == null)
        {
            var hudObject = new GameObject("GameHUD");
            hudObject.AddComponent<GameHUD>();
        }

        GenerateCourse(root, player);
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

    void GenerateCourse(Transform root, PlayerController player)
    {
        if (root == null)
        {
            return;
        }

        int targetCount = Mathf.Clamp(Random.Range(minPlatforms, maxPlatforms + 1), minPlatforms, maxPlatforms);
        if (targetCount <= 0)
        {
            return;
        }

        Vector3 anchor = Vector3.zero;
        if (player != null)
        {
            anchor = player.respawnPoint != null ? player.respawnPoint.position : player.transform.position;
        }

        float currentHeight = Mathf.Max(anchor.y + 0.8f, 1f);
        float currentForward = anchor.z + 4f;
        float baseX = anchor.x;

        var platforms = new List<GameObject>(targetCount);

        for (int i = 0; i < targetCount; i++)
        {
            float heightStep = Random.Range(heightStepRange.x, heightStepRange.y);
            if (i == 0)
            {
                heightStep = Mathf.Clamp(heightStep, heightStepRange.x * 0.5f, heightStepRange.y);
            }

            currentHeight += heightStep;
            currentForward += Random.Range(forwardStepRange.x, forwardStepRange.y);

            float lateral = baseX + Random.Range(-lateralSpread, lateralSpread);
            Vector3 position = new Vector3(lateral, currentHeight, currentForward);
            float sizeX = Random.Range(platformSizeRange.x, platformSizeRange.y);
            float sizeZ = Random.Range(platformSizeRange.x, platformSizeRange.y);
            Vector3 scale = new Vector3(sizeX, platformThickness, sizeZ);

            GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
            platform.name = $"Platform_{i:00}";
            platform.transform.SetParent(root, false);
            platform.transform.position = position;
            platform.transform.localScale = scale;

            platforms.Add(platform);
        }

        if (platforms.Count == 0)
        {
            return;
        }

        int movingIndex = SetupMovingPlatform(platforms, root);
        SetupIcePlatform(platforms, movingIndex);
        SetupCheckpoints(platforms, root);
    }

    int SetupMovingPlatform(List<GameObject> platforms, Transform root)
    {
        if (platforms.Count < 2)
        {
            return -1;
        }

        int movingIndex = Mathf.Clamp(platforms.Count / 2, 1, platforms.Count - 1);
        GameObject platform = platforms[movingIndex];
        var mover = platform.AddComponent<MovingPlatform>();

        Vector3 basePosition = platform.transform.position;
        Vector3 travel = new Vector3(2.5f, 0f, 0f);

        var pointAObj = new GameObject(platform.name + "_PointA");
        pointAObj.transform.SetParent(root, false);
        pointAObj.transform.position = basePosition - travel * 0.5f;

        var pointBObj = new GameObject(platform.name + "_PointB");
        pointBObj.transform.SetParent(root, false);
        pointBObj.transform.position = basePosition + travel * 0.5f;

        mover.pointA = pointAObj.transform;
        mover.pointB = pointBObj.transform;
        mover.speed = 2.2f;
        mover.startAtPointA = true;

        return movingIndex;
    }

    void SetupIcePlatform(List<GameObject> platforms, int movingIndex)
    {
        if (platforms.Count == 0)
        {
            return;
        }

        int iceIndex = Mathf.Max(0, platforms.Count - 2);
        if (iceIndex == movingIndex)
        {
            iceIndex = Mathf.Max(0, iceIndex - 1);
        }

        GameObject icePlatform = platforms[Mathf.Clamp(iceIndex, 0, platforms.Count - 1)];
        var collider = icePlatform.GetComponent<Collider>();
        EnsurePhysicsMaterials.ApplyIce(collider);

        var renderer = icePlatform.GetComponent<Renderer>();
        if (renderer != null)
        {
            var mat = renderer.material;
            mat.color = new Color(0.6f, 0.8f, 1f, 1f);
        }
    }

    void SetupCheckpoints(List<GameObject> platforms, Transform root)
    {
        if (platforms.Count < 2)
        {
            return;
        }

        var indices = new HashSet<int>();
        indices.Add(1);
        indices.Add(platforms.Count / 2);
        if (platforms.Count > 4)
        {
            indices.Add(Mathf.Max(1, platforms.Count - 2));
        }

        foreach (int index in indices)
        {
            if (index < 0 || index >= platforms.Count)
            {
                continue;
            }

            GameObject platform = platforms[index];
            Vector3 platformTop = platform.transform.position + Vector3.up * (platform.transform.localScale.y * 0.5f + 0.8f);

            GameObject checkpointRoot = new GameObject($"Checkpoint_{index:00}");
            checkpointRoot.transform.SetParent(root, false);
            checkpointRoot.transform.position = platformTop;
            checkpointRoot.transform.localScale = Vector3.one * 0.8f;

            var trigger = checkpointRoot.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 0.6f;
            checkpointRoot.AddComponent<Checkpoint>();

            CreateCheckpointVisual(checkpointRoot.transform);
        }
    }

    void CreateCheckpointVisual(Transform parent)
    {
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        visual.name = "Marker";
        visual.transform.SetParent(parent, false);
        visual.transform.localScale = new Vector3(0.3f, 1.2f, 0.3f);

        var renderer = visual.GetComponent<Renderer>();
        if (renderer != null)
        {
            var mat = renderer.material;
            mat.color = new Color(1f, 0.8f, 0.2f, 1f);
        }

        var collider = visual.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }
    }
}
