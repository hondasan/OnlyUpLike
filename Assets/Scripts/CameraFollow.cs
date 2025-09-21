using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;        // 追従対象（Player）
    public Vector3 offset = new Vector3(0, 3, -6); // 相対位置
    public float smoothSpeed = 5f;  // 補間の速さ

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.LookAt(target);
    }
}
