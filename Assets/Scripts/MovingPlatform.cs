using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    public Transform pointA;
    public Transform pointB;
    public float speed = 2f;
    public bool startAtPointA = true;

    Vector3 targetPosition;
    bool movingToB;
    bool warnedMissingPoints;

    void Start()
    {
        if (!HasValidPoints())
        {
            return;
        }

        movingToB = startAtPointA;
        transform.position = startAtPointA ? pointA.position : pointB.position;
        targetPosition = movingToB ? pointB.position : pointA.position;
    }

    void Update()
    {
        if (!HasValidPoints())
        {
            return;
        }

        float step = speed * Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, step);

        if (Vector3.SqrMagnitude(transform.position - targetPosition) < 0.0001f)
        {
            movingToB = !movingToB;
            targetPosition = movingToB ? pointB.position : pointA.position;
        }
    }

    bool HasValidPoints()
    {
        if (pointA == null || pointB == null)
        {
            if (!warnedMissingPoints)
            {
                Debug.LogWarning("MovingPlatform requires both pointA and pointB.", this);
                warnedMissingPoints = true;
            }
            return false;
        }

        warnedMissingPoints = false;
        return true;
    }

    void OnDrawGizmosSelected()
    {
        if (pointA == null || pointB == null)
        {
            return;
        }

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(pointA.position, pointB.position);
        Gizmos.DrawSphere(pointA.position, 0.1f);
        Gizmos.DrawSphere(pointB.position, 0.1f);
    }
}
