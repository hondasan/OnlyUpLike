using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SFXManager : MonoBehaviour
{
    public static SFXManager Instance { get; private set; }

    [Header("Clips")]
    public AudioClip jumpClip;
    public AudioClip landClip;
    public AudioClip hardLandClip;
    public AudioClip respawnClip;
    public AudioClip checkpointClip;
    public AudioClip trampolineClip;
    public AudioClip breakStartClip;

    [Header("Settings")]
    public float baseVolume = 1f;
    public bool spatialize;
    public float spatialBlend = 0.25f;

    AudioSource audioSource;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = spatialize ? Mathf.Clamp01(spatialBlend) : 0f;
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void PlayJump(Vector3 position)
    {
        PlayClip(jumpClip, position, baseVolume);
    }

    public void PlayLand(Vector3 position, float impactSpeed)
    {
        AudioClip clip = impactSpeed >= 8f && hardLandClip != null ? hardLandClip : landClip;
        float volume = baseVolume * Mathf.Clamp01(0.4f + impactSpeed / 18f);
        PlayClip(clip, position, volume);
    }

    public void PlayRespawn(Vector3 position)
    {
        PlayClip(respawnClip, position, baseVolume);
    }

    public void PlayCheckpoint(Vector3 position)
    {
        PlayClip(checkpointClip, position, baseVolume);
    }

    public void PlayTrampoline(Vector3 position)
    {
        PlayClip(trampolineClip, position, baseVolume);
    }

    public void PlayBreakStart(Vector3 position)
    {
        PlayClip(breakStartClip, position, baseVolume);
    }

    void PlayClip(AudioClip clip, Vector3 position, float volume)
    {
        if (clip == null || audioSource == null)
        {
            return;
        }

        audioSource.transform.position = position;
        audioSource.PlayOneShot(clip, Mathf.Clamp01(volume));
    }
}
