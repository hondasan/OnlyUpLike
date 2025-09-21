using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Move")]
    public float moveForce = 35f;   // 押し出し力
    public float maxSpeed  = 6f;    // 水平の最高速度

    [Header("Jump")]
    public float jumpForce = 7f;    // ジャンプ力
    public float groundCheckRadius = 0.25f;
    public float groundCheckOffset = 0.6f;
    public LayerMask groundMask = ~0; // 迷ったらこのまま（全部のレイヤー）

    [Header("Respawn")]
    public float fallY = -10f;          // この高さより下に落ちたら
    public Transform respawnPoint;      // 戻る地点

    [Header("Recovery")]
    [Tooltip("リスポーン直後の入力ロック時間")]
    public float inputDisableDuration = 0.15f; // --- Added for respawn stability

    Rigidbody rb;
    Vector3 inputDir;
    bool isGrounded;

    // --- Added fields for respawn stability ---
    Vector3 initialPosition;
    Quaternion initialRotation;
    bool inputLocked;
    Coroutine inputLockRoutine;
    int fallCount;

    // --- Added for HUD hookup ---
    public event System.Action<int> Respawned;
    public int FallCount => fallCount;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        initialPosition = transform.position;
        initialRotation = transform.rotation;
    }

    void Update()
    {
        // 入力（WASD / 矢印）
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        // カメラ基準で移動方向を決めると直感的
        var cam = Camera.main;
        Vector3 camF = cam != null ? cam.transform.forward : Vector3.forward;
        Vector3 camR = cam != null ? cam.transform.right : Vector3.right;
        camF.y = 0; camF.Normalize();
        camR.y = 0; camR.Normalize();

        inputDir = inputLocked ? Vector3.zero : (camF * v + camR * h).normalized;

        // 接地判定（足元に球を置く）
        LayerMask mask = groundMask.value == 0 ? ~0 : groundMask;
        float radius = Mathf.Max(0.01f, groundCheckRadius);
        float extraOffset = Mathf.Max(radius, 0.05f);
        Vector3 checkCenter = transform.position + Vector3.down * (groundCheckOffset + extraOffset);
        isGrounded = Physics.CheckSphere(checkCenter, radius, mask, QueryTriggerInteraction.Ignore);

        // ジャンプ
        if (!inputLocked && Input.GetButtonDown("Jump") && isGrounded)
        {
            var v0 = rb.linearVelocity; if (v0.y < 0) v0.y = 0; rb.linearVelocity = v0;
            rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
        }

        // 水平速度の上限
        var vel = rb.linearVelocity;
        var horiz = new Vector3(vel.x, 0, vel.z);
        if (horiz.magnitude > maxSpeed)
        {
            horiz = horiz.normalized * maxSpeed;
            rb.linearVelocity = new Vector3(horiz.x, vel.y, horiz.z);
        }

        // 落下チェック
        if (transform.position.y < fallY)
        {
            Respawn();
        }
    }

    void FixedUpdate()
    {
        // 毎フレーム 少しずつ押す
        rb.AddForce(inputDir * moveForce, ForceMode.Acceleration);
    }

    // エディタで当たり判定の目印
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        float radius = Mathf.Max(0.01f, groundCheckRadius);
        float extraOffset = Mathf.Max(radius, 0.05f);
        Vector3 checkCenter = transform.position + Vector3.down * (groundCheckOffset + extraOffset);
        Gizmos.DrawWireSphere(checkCenter, radius);
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
            // 保険：RespawnPointが未設定でも初期位置に戻す
            targetPosition = initialPosition;
            targetRotation = initialRotation;
        }

        // --- Added resets for stability ---
        rb.linearVelocity = Vector3.zero;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        transform.SetPositionAndRotation(targetPosition, targetRotation);
        inputDir = Vector3.zero;
        isGrounded = false;

        fallCount++;
        Respawned?.Invoke(fallCount);

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
}
