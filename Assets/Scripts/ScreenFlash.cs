using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ScreenFlash : MonoBehaviour
{
    public Image overlayImage;

    Coroutine activeFlash;

    void Awake()
    {
        if (overlayImage == null)
        {
            overlayImage = GetComponent<Image>();
        }

        if (overlayImage == null)
        {
            overlayImage = GetComponentInChildren<Image>();
        }

        if (overlayImage != null)
        {
            var color = overlayImage.color;
            color.a = 0f;
            overlayImage.color = color;
        }
    }

    public void Flash(Color color, float duration)
    {
        if (overlayImage == null)
        {
            Debug.LogWarning("ScreenFlash requires an Image to flash.", this);
            return;
        }

        if (activeFlash != null)
        {
            StopCoroutine(activeFlash);
        }

        activeFlash = StartCoroutine(FlashRoutine(color, Mathf.Max(0.05f, duration)));
    }

    IEnumerator FlashRoutine(Color color, float duration)
    {
        overlayImage.gameObject.SetActive(true);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float alpha = Mathf.Lerp(color.a, 0f, t);
            overlayImage.color = new Color(color.r, color.g, color.b, alpha);
            yield return null;
        }

        overlayImage.color = new Color(color.r, color.g, color.b, 0f);
        activeFlash = null;
    }
}
