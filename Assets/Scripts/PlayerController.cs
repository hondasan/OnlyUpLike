using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class PlayerController : MonoBehaviour
{
    [Header("Move")]
    public float moveForce = 35f;
    public float maxSpeed = 6f;
    [Range(0f, 1f)] public float airControlMultiplier = 0.6f;

    [Header("Jump")]
    public float jumpForce = 7.2f;

    // 地面判定（コライダー底面からの予備距離は極小に）
    [Tooltip("地面とみなす余白（底面からの追加距離）。極小にするほど二段ジャンプが起きにくい")]
    public float groundSkin = 0.04f;           // ★ 足元の余白
    [Tooltip("接地→離陸直後に誤検知しないためのロック時間")]
    public float groundLockAfterJump = 0.20f;  // ★ ジャンプ直後は必ず空中扱い
    [Tooltip("接地時に上向き速度がある場合はジャンプを禁止する閾値")]
    public float upwardVelBlock = 0.05f;       // ★ 上向き速度が残っているときは跳べない

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

    // --- runtime ---
    Rigidbody rb;
    Collider col;                     // ★ 追加：正確な底面判定に使用
    Vector3 inputDir;
    bool isGrounded;
    bool wasGrounded;
    bool inputLocked;
    Coroutine inputLockRoutine;
    Vector3 initialPosition;
    Quaternion initialRotation;
    int fallCount;
    float lowestVerticalVelocity;
    float jumpGroundLockTimer;        // ★ ジャンプ直後の接地無効タイマー
    SFXManager sfx;

    public event System.Action<int> Respawned;
    public event System.Action Jumped;
    public event System.Action<float> Landed;
    public int FallCount => fallCount;

    void Awake()
    {
        rb  = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();     // ★ 取得
        initialPosition = transform.position;
        initialRotation = transform.rotation;
        AcquireFeedbackComponents();
    }

    void AcquireFeedbackComponents()
    {
        if (cameraShake == null)
        {
            var cam = Camera.main;
            if (cam != null) cameraShake = cam.GetComponent<CameraShake>();
            if (cameraShake == null) cameraShake = FindObjectOfType<CameraShake>();
        }
        if (screenFlash == null) screenFlash = FindObjectOfType<ScreenFlash>();
        sfx = SFXManager.Instance ?? FindObjectOfType<SFXManager>();
    }

    void Update()
    {
        AcquireFeedbackComponents();

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        var cam = Camera.main;
        Vector3 camF = cam ? cam.transform.forward : Vector3.forward;
        Vector3 camR = cam ? cam.transform.right  : Vector3.right;
        camF.y = 0; camF.Normalize(); camR.y = 0; camR.Normalize();

        Vector3 desired = camF * v + camR * h;
        if (desired.sqrMagnitude > 1f) desired.Normalize();
        inputDir = inputLocked ? Vector3.zero : desired;

        UpdateGroundedState();   // ← 接地の更新を先に
        HandleJump();            // ← その情報を使ってジャンプ
        LimitHorizontalSpeed();
        CheckFallOutOfWorld();
    }

    void FixedUpdate()
    {
        Vector3 force = isGrounded ? inputDir : inputDir * airControlMultiplier;
        rb.AddForce(force * moveForce, ForceMode.Acceleration);
    }

    // === 接地判定（コライダー底面から極短Ray）=========================
    void UpdateGroundedState()
    {
        if (jumpGroundLockTimer > 0f)
        {
            jumpGroundLockTimer -= Time.deltaTime;
            isGrounded = false;                 // ★ ジャンプ直後は強制で空中扱い
        }
        else
        {
            isGrounded = ProbeGroundByBounds();
        }

        // 着地イベント
        if (!wasGrounded && isGrounded)
        {
            float impactSpeed = Mathf.Abs(lowestVerticalVelocity);
            if (impactSpeed > 0.1f)
            {
                Landed?.Invoke(impactSpeed);
                TriggerLandingFeedback(impactSpeed);
            }
            lowestVerticalVelocity = 0f;
        }

        // 空中中は最も低いY速度を記録（着地強度に利用）
        if (!isGrounded && rb.linearVelocity.y < lowestVerticalVelocity)
            lowestVerticalVelocity = rb.linearVelocity.y;

        wasGrounded = isGrounded;
    }

    bool ProbeGroundByBounds()
    {
        // コライダーの底面の少し上を原点に、真下へごく短い Ray を飛ばす
        Bounds b = col.bounds;
        Vector3 origin = new Vector3(b.center.x, b.min.y + 0.02f, b.center.z); // 底面より少し上
        float rayLen = groundSkin + 0.02f;                                      // ほんの少しだけ下を見る

        // 地面にのみ当てたい場合はレイヤーを使う（未設定なら全レイヤー）
        int mask = ~0;
        return Physics.Raycast(origin, Vector3.down, rayLen, mask, QueryTriggerInteraction.Ignore);
    }
    // ====================================================================

    void HandleJump()
    {
        if (inputLocked) return;

        // ★ 重要：接地 & 上向き速度ほぼゼロ のときだけ
        if (isGrounded && rb.linearVelocity.y <= upwardVelBlock && Input.GetButtonDown("Jump"))
        {
            Vector3 v = rb.linearVelocity;
            if (v.y < 0f) v.y = 0f;                  // 落下中なら上向きに戻す
            rb.linearVelocity = new Vector3(v.x, 0f, v.z); // 上方向をリセット
            rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);

            // ジャンプ直後は必ず“非接地”として一定時間ロック
            jumpGroundLockTimer = groundLockAfterJump;   // ★ 0.20〜0.25 推奨
            isGrounded = false;
            wasGrounded = false;
            lowestVerticalVelocity = 0f;

            Jumped?.Invoke();
            if (sfx) sfx.PlayJump(transform.position);
        }
    }

    void LimitHorizontalSpeed()
    {
        Vector3 v = rb.linearVelocity;
        Vector3 h = new Vector3(v.x, 0f, v.z);
        float max = Mathf.Max(0.1f, maxSpeed);
        if (h.sqrMagnitude > max * max)
        {
            h = h.normalized * max;
            rb.linearVelocity = new Vector3(h.x, v.y, h.z);
        }
    }

    void CheckFallOutOfWorld()
    {
        if (transform.position.y < fallY) Respawn();
    }

    void TriggerLandingFeedback(float impactSpeed)
    {
        if (sfx) sfx.PlayLand(transform.position, impactSpeed);
        float amp = Mathf.Clamp(impactSpeed * landingShakeScale, 0.03f, 0.25f);
        if (cameraShake)
        {
            float dur = impactSpeed >= hardLandingThreshold ? 0.35f : 0.2f;
            cameraShake.ShakeOnce(dur, amp);
        }
    }

    void Respawn()
    {
        Vector3 p; Quaternion r;
        if (respawnPoint)
        {
            p = respawnPoint.position; r = respawnPoint.rotation;
        }
        else
        {
            p = initialPosition; r = initialRotation;
        }

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        transform.SetPositionAndRotation(p, r);
        inputDir = Vector3.zero;

        // 復帰後もしばらくは空中扱い（誤検知防止）
        isGrounded = false; wasGrounded = false;
        lowestVerticalVelocity = 0f;
        jumpGroundLockTimer = 0.2f;

        fallCount++;
        Respawned?.Invoke(fallCount);

        if (sfx) sfx.PlayRespawn(transform.position);
        if (screenFlash) screenFlash.Flash(respawnFlashColor, 0.6f);
        if (cameraShake) cameraShake.ShakeOnce(0.35f, 0.18f);

        if (inputLockRoutine != null) StopCoroutine(inputLockRoutine);
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
        if (!TryGetComponent(out Collider c)) return;
        Gizmos.color = Color.yellow;
        Bounds b = c.bounds;
        Vector3 origin = new Vector3(b.center.x, b.min.y + 0.02f, b.center.z);
        Gizmos.DrawLine(origin, origin + Vector3.down * (groundSkin + 0.02f));
    }
}
