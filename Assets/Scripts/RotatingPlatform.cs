using UnityEngine;

public class RotatingPlatform : MonoBehaviour
{
    public Vector3 axis = Vector3.up;
    public float degreesPerSecond = 60f;
    public Space space = Space.Self;
    public bool useUnscaledTime;

    void Update()
    {
        if (axis.sqrMagnitude < 0.0001f || Mathf.Approximately(degreesPerSecond, 0f))
        {
            return;
        }

        float delta = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        float angle = degreesPerSecond * delta;
        Vector3 normalizedAxis = axis.normalized;
        transform.Rotate(normalizedAxis, angle, space);
    }
}
