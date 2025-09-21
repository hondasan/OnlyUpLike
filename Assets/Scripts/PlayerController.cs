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


    Rigidbody rb;
    Vector3 inputDir;
    bool isGrounded;

    void Awake() => rb = GetComponent<Rigidbody>();

    void Update()
    {
        // 入力（WASD / 矢印）
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        // カメラ基準で移動方向を決めると直感的
        var camF = Camera.main.transform.forward; camF.y = 0; camF.Normalize();
        var camR = Camera.main.transform.right;  camR.y = 0; camR.Normalize();
        inputDir = (camF * v + camR * h).normalized;

        // 接地判定（足元に小さな球）
        Vector3 origin = transform.position + Vector3.up * groundCheckOffset;
        isGrounded = Physics.CheckSphere(origin + Vector3.down * groundCheckOffset, groundCheckRadius, groundMask);

        // ジャンプ
        if (Input.GetButtonDown("Jump") && isGrounded)
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
        Vector3 origin = transform.position + Vector3.up * groundCheckOffset;
        Gizmos.DrawWireSphere(origin + Vector3.down * groundCheckOffset, groundCheckRadius);
    }

    void Respawn()
    {
        if (respawnPoint != null)
        {
            rb.linearVelocity = Vector3.zero;
            transform.position = respawnPoint.position;
            transform.rotation = respawnPoint.rotation;
        }
        else
        {
            // 保険：RespawnPointが未設定でも初期位置に戻す
            rb.linearVelocity = Vector3.zero;
            transform.position = new Vector3(0, 1, 0);
        }
    }

}
