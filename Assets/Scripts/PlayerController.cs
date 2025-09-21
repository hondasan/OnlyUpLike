using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Move")]
    public float moveForce = 35f;
    public float maxSpeed = 6f;
    [Range(0f, 1f)] public float airControlMultiplier = 0.6f;

    [Header("Jump")]
    public float jumpForce = 7.2f;
    public float groundCheckRadius = 0.35f;
    public float groundCheckOffset = 0.9f;
    public float groundCheckDistance = 0.25f;
    public LayerMask groundMask = ~0;

    [Header("Respawn")]
    public float fallY = -10f;
    public Transform respawnPoint;

    [Header("Recovery")]
    [Tooltip("リスポーン直後の入力ロック時間")]
    public float inputDisableDuration = 0.2f;

    [Header("Feedback")]
    public CameraShake cameraShake;
    public ScreenFlash screenFlash;
    public Color respawnFlashColor = new Color(1f, 1f, 1f, 0.35f);
    public float landingShakeScale = 0.02f;
    public float hardLandingThreshold = 8f;

    Rigidbody rb;
    Vector3 inputDir;
    bool isGrounded;
    bool wasGrounded;
    bool inputLocked;
    Coroutine inputLockRoutine;
    Vector3 initialPosition;
    Quaternion initialRotation;
    int fallCount;
    float groundResetTimer;
    float lowestVerticalVelocity;
    SFXManager sfx;

    public event System.Action<int> Respawned;
    public event System.Action Jumped;
    public event System.Action<float> Landed;

    public int FallCount => fallCount;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        initialPosition = transform.position;
        initialRotation = transform.rotation;
        AcquireFeedbackComponents();
    }

    void AcquireFeedbackComponents()
    {
        if (cameraShake == null)
        {
            var cam = Camera.main;
            if (cam != null)
            {
                cameraShake = cam.GetComponent<CameraShake>();
            }
            if (cameraShake == null)
            {
                cameraShake = FindObjectOfType<CameraShake>();
            }
        }

        if (screenFlash == null)
        {
            screenFlash = FindObjectOfType<ScreenFlash>();
        }

        sfx = SFXManager.Instance ?? FindObjectOfType<SFXManager>();
    }

    void Update()
    {
        AcquireFeedbackComponents();

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        var cam = Camera.main;
        Vector3 camF = cam != null ? cam.transform.forward : Vector3.forward;
        Vector3 camR = cam != null ? cam.transform.right : Vector3.right;
        camF.y = 0f; camF.Normalize();
        camR.y = 0f; camR.Normalize();

        Vector3 desired = (camF * v + camR * h);
        if (desired.sqrMagnitude > 1f)
        {
            desired.Normalize();
        }

        inputDir = inputLocked ? Vector3.zero : desired;

        UpdateGroundedState();
        HandleJump();
        LimitHorizontalSpeed();
        CheckFallOutOfWorld();
    }

    void FixedUpdate()
    {
        Vector3 forceDirection = inputDir;
        if (!isGrounded)
        {
            forceDirection *= airControlMultiplier;
        }

        rb.AddForce(forceDirection * moveForce, ForceMode.Acceleration);
    }

    void UpdateGroundedState()
    {
        bool previousGrounded = wasGrounded;
        bool groundedNow;

        if (groundResetTimer > 0f)
        {
            groundResetTimer -= Time.deltaTime;
            groundedNow = false;
        }
        else
        {
            groundedNow = ProbeGround();
        }

        Vector3 velocity = rb.velocity;

        if (!groundedNow)
        {
            if (velocity.y < lowestVerticalVelocity)
            {
                lowestVerticalVelocity = velocity.y;
            }
        }
        else
        {
            if (!previousGrounded)
            {
                float impactSpeed = Mathf.Abs(lowestVerticalVelocity);
                if (impactSpeed > 0.1f)
                {
                    Landed?.Invoke(impactSpeed);
                    TriggerLandingFeedback(impactSpeed);
                }
            }

            lowestVerticalVelocity = 0f;
        }

        wasGrounded = groundedNow;
        isGrounded = groundedNow;
    }

    bool ProbeGround()
    {
        float radius = Mathf.Max(0.05f, groundCheckRadius);
        float offset = Mathf.Max(0.1f, groundCheckOffset);
        float distance = Mathf.Max(0.05f, groundCheckDistance);
        int mask = groundMask.value == 0 ? ~0 : groundMask.value;

        Vector3 origin = transform.position + Vector3.up * (radius + 0.05f);
        if (Physics.SphereCast(origin, radius, Vector3.down, out RaycastHit hit, offset + distance, mask, QueryTriggerInteraction.Ignore))
        {
            return true;
        }

        Vector3 checkCenter = transform.position + Vector3.down * offset;
        return Physics.CheckSphere(checkCenter, radius * 0.95f, mask, QueryTriggerInteraction.Ignore);
    }

    void HandleJump()
    {
        if (inputLocked)
        {
            return;
        }

        if (isGrounded && Input.GetButtonDown("Jump"))
        {
            Vector3 velocity = rb.velocity;
            if (velocity.y < 0f)
            {
                velocity.y = 0f;
            }

            rb.velocity = new Vector3(velocity.x, 0f, velocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
            groundResetTimer = 0.12f;
            isGrounded = false;
            wasGrounded = false;
            lowestVerticalVelocity = 0f;

            Jumped?.Invoke();
            if (sfx != null)
            {
                sfx.PlayJump(transform.position);
            }
        }
    }

    void LimitHorizontalSpeed()
    {
        Vector3 velocity = rb.velocity;
        Vector3 horizontal = new Vector3(velocity.x, 0f, velocity.z);
        float max = Mathf.Max(0.1f, maxSpeed);

        if (horizontal.sqrMagnitude > max * max)
        {
            horizontal = horizontal.normalized * max;
            rb.velocity = new Vector3(horizontal.x, velocity.y, horizontal.z);
        }
    }

    void CheckFallOutOfWorld()
    {
        if (transform.position.y < fallY)
        {
            Respawn();
        }
    }

    void TriggerLandingFeedback(float impactSpeed)
    {
        if (sfx != null)
        {
            sfx.PlayLand(transform.position, impactSpeed);
        }

        float amplitude = Mathf.Clamp(impactSpeed * landingShakeScale, 0.03f, 0.25f);
        if (cameraShake != null)
        {
            float duration = impactSpeed >= hardLandingThreshold ? 0.35f : 0.2f;
            cameraShake.ShakeOnce(duration, amplitude);
        }
    }

    void Respawn()
    {
        Vector3 targetPosition;
        Quaternion targetRotation;

        if (respawnPoint != null)
        {
            targetPosition = respawnPoint.position;
            targetRotation = respawnPoint.rotation;
        }
        else
        {
            targetPosition = initialPosition;
            targetRotation = initialRotation;
        }

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        transform.SetPositionAndRotation(targetPosition, targetRotation);
        inputDir = Vector3.zero;
        isGrounded = false;
        wasGrounded = false;
        lowestVerticalVelocity = 0f;
        groundResetTimer = 0.2f;

        fallCount++;
        Respawned?.Invoke(fallCount);

        if (sfx != null)
        {
            sfx.PlayRespawn(transform.position);
        }

        if (screenFlash != null)
        {
            screenFlash.Flash(respawnFlashColor, 0.6f);
        }

        if (cameraShake != null)
        {
            cameraShake.ShakeOnce(0.35f, 0.18f);
        }

        if (inputLockRoutine != null)
        {
            StopCoroutine(inputLockRoutine);
        }
        inputLockRoutine = StartCoroutine(DisableInputTemporarily(inputDisableDuration));
    }

    IEnumerator DisableInputTemporarily(float duration)
    {
        inputLocked = true;
        yield return new WaitForSeconds(duration);
        inputLocked = false;
        inputLockRoutine = null;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        float radius = Mathf.Max(0.05f, groundCheckRadius);
        float offset = Mathf.Max(0.1f, groundCheckOffset);
        Vector3 center = transform.position + Vector3.down * offset;
        Gizmos.DrawWireSphere(center, radius);

        Vector3 origin = transform.position + Vector3.up * (radius + 0.05f);
        Gizmos.DrawLine(origin, origin + Vector3.down * (offset + Mathf.Max(0.05f, groundCheckDistance)));
    }
}
