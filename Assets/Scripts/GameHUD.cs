using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameHUD : MonoBehaviour
{
    [SerializeField] PlayerController player;
    [SerializeField] TextMeshProUGUI displayText;

    float startTime;
    int recordedFalls;
    bool subscribed;

    void Awake()
    {
        TryFindPlayer();
        EnsureUi();
    }

    void OnEnable()
    {
        startTime = Time.time;
        Subscribe();
    }

    void OnDisable()
    {
        Unsubscribe();
    }

    void OnDestroy()
    {
        Unsubscribe();
    }

    void Update()
    {
        if (player == null)
        {
            TryFindPlayer();
            Subscribe();
        }

        if (displayText == null || player == null)
        {
            return;
        }

        float height = player.transform.position.y;
        TimeSpan elapsed = TimeSpan.FromSeconds(Mathf.Max(0f, Time.time - startTime));
        displayText.text = $"Height: {height:0.0}m   Falls: {recordedFalls}   Time: {elapsed:hh\\:mm\\:ss}";
    }

    void TryFindPlayer()
    {
        if (player == null)
        {
            player = FindObjectOfType<PlayerController>();
            if (player != null)
            {
                recordedFalls = player.FallCount;
            }
        }
    }

    void Subscribe()
    {
        if (player != null && !subscribed)
        {
            player.Respawned += OnPlayerRespawned;
            recordedFalls = player.FallCount;
            subscribed = true;
        }
    }

    void Unsubscribe()
    {
        if (player != null && subscribed)
        {
            player.Respawned -= OnPlayerRespawned;
            subscribed = false;
        }
    }

    void OnPlayerRespawned(int count)
    {
        recordedFalls = count;
    }

    void EnsureUi()
    {
        if (displayText != null)
        {
            return;
        }

        Canvas canvas = GetComponentInChildren<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("HUD Canvas");
            canvasObj.transform.SetParent(transform, false);
            canvasObj.layer = gameObject.layer;

            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasObj.AddComponent<GraphicRaycaster>();
        }

        GameObject textObj = new GameObject("HUD Text");
        textObj.transform.SetParent(canvas.transform, false);
        displayText = textObj.AddComponent<TextMeshProUGUI>();
        displayText.enableWordWrapping = false;
        displayText.fontSize = 32f;
        displayText.alignment = TextAlignmentOptions.TopLeft;
        displayText.text = "Height: 0.0m   Falls: 0   Time: 00:00:00";

        var rect = displayText.rectTransform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(24f, -24f);
    }
}
