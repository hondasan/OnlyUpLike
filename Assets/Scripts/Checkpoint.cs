using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Checkpoint : MonoBehaviour
{
    [Header("Checkpoint Visuals")]
    public Color inactiveColor = new Color(1f, 0.5f, 0f, 0.35f);
    public Color activeColor = new Color(0.2f, 0.8f, 1f, 0.6f);

    bool activated;

    void Reset()
    {
        SetTrigger();
    }

    void OnValidate()
    {
        SetTrigger();
    }

    void SetTrigger()
    {
        var col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        var player = other.GetComponent<PlayerController>() ?? other.GetComponentInParent<PlayerController>();
        if (player == null)
        {
            return;
        }

        player.respawnPoint = transform;
        activated = true;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = activated ? activeColor : inactiveColor;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(Vector3.zero, Vector3.one);
    }
}
