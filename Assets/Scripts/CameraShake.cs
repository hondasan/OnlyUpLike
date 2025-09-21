using System.Collections;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    public float noiseFrequency = 18f;
    public float recoverySpeed = 10f;

    Coroutine activeShake;
    Vector3 originalLocalPosition;
    Quaternion originalLocalRotation;

    void Awake()
    {
        originalLocalPosition = transform.localPosition;
        originalLocalRotation = transform.localRotation;
    }

    void OnDisable()
    {
        ResetTransform();
    }

    public void ShakeOnce(float duration, float amplitude)
    {
        if (duration <= 0f || amplitude <= 0f)
        {
            return;
        }

        if (activeShake != null)
        {
            StopCoroutine(activeShake);
        }

        activeShake = StartCoroutine(ShakeRoutine(Mathf.Max(0.01f, duration), Mathf.Abs(amplitude)));
    }

    IEnumerator ShakeRoutine(float duration, float amplitude)
    {
        float elapsed = 0f;
        Vector3 seed = new Vector3(Random.value * 100f, Random.value * 100f, Random.value * 100f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float falloff = 1f - t;
            float time = Time.time * noiseFrequency;

            float x = (Mathf.PerlinNoise(seed.x, time) - 0.5f) * 2f;
            float y = (Mathf.PerlinNoise(seed.y, time) - 0.5f) * 2f;
            float z = (Mathf.PerlinNoise(seed.z, time) - 0.5f) * 2f;

            Vector3 offset = new Vector3(x, y, z) * amplitude * falloff;
            transform.localPosition = originalLocalPosition + offset;
            transform.localRotation = originalLocalRotation;
            yield return null;
        }

        float recoverTime = 0f;
        while (recoverTime < 1f)
        {
            recoverTime += Time.deltaTime * recoverySpeed;
            float t = Mathf.Clamp01(recoverTime);
            transform.localPosition = Vector3.Lerp(transform.localPosition, originalLocalPosition, t);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, originalLocalRotation, t);
            yield return null;
        }

        ResetTransform();
        activeShake = null;
    }

    void ResetTransform()
    {
        transform.localPosition = originalLocalPosition;
        transform.localRotation = originalLocalRotation;
    }
}
