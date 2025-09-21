using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class BreakablePlatform : MonoBehaviour
{
    public float delay = 0.75f;
    public float cooldown = 3f;
    public bool disableRendererOnBreak = true;

    Collider[] colliders;
    Renderer[] renderers;
    Vector3 initialPosition;
    Quaternion initialRotation;
    Vector3 initialScale;
    bool breaking;
    bool broken;
    float lastTriggerTime;

    void Awake()
    {
        colliders = GetComponentsInChildren<Collider>();
        renderers = GetComponentsInChildren<Renderer>();
        CacheTransform();
    }

    void OnEnable()
    {
        ResetPlatform();
    }

    void CacheTransform()
    {
        initialPosition = transform.position;
        initialRotation = transform.rotation;
        initialScale = transform.localScale;
    }

    void ResetPlatform()
    {
        if (colliders != null)
        {
            foreach (var col in colliders)
            {
                if (col != null)
                {
                    col.enabled = true;
                }
            }
        }

        if (disableRendererOnBreak && renderers != null)
        {
            foreach (var rend in renderers)
            {
                if (rend != null)
                {
                    rend.enabled = true;
                }
            }
        }

        breaking = false;
        broken = false;
        lastTriggerTime = 0f;
        transform.SetPositionAndRotation(initialPosition, initialRotation);
        transform.localScale = initialScale;
    }

    void OnCollisionEnter(Collision collision)
    {
        EvaluateTrigger(collision.collider);
    }

    void OnCollisionStay(Collision collision)
    {
        EvaluateTrigger(collision.collider);
    }

    void OnTriggerEnter(Collider other)
    {
        EvaluateTrigger(other);
    }

    void EvaluateTrigger(Collider other)
    {
        if (breaking || broken || other == null)
        {
            return;
        }

        if (Time.time - lastTriggerTime < 0.1f)
        {
            return;
        }

        var player = other.GetComponent<PlayerController>() ?? other.GetComponentInParent<PlayerController>();
        if (player == null)
        {
            return;
        }

        lastTriggerTime = Time.time;
        StartCoroutine(BreakRoutine());
    }

    IEnumerator BreakRoutine()
    {
        breaking = true;

        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        if (colliders != null)
        {
            foreach (var col in colliders)
            {
                if (col != null)
                {
                    col.enabled = false;
                }
            }
        }

        if (disableRendererOnBreak && renderers != null)
        {
            foreach (var rend in renderers)
            {
                if (rend != null)
                {
                    rend.enabled = false;
                }
            }
        }

        broken = true;

        var sfx = SFXManager.Instance;
        if (sfx != null)
        {
            sfx.PlayBreakStart(transform.position);
        }

        if (cooldown > 0f)
        {
            yield return new WaitForSeconds(cooldown);
        }

        ResetPlatform();
    }
}
