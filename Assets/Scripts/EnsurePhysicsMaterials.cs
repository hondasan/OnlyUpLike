using UnityEngine;

public class EnsurePhysicsMaterials : MonoBehaviour
{
    static PhysicsMaterial iceMaterial;

    [Tooltip("設定したColliderにPM_Iceを適用します。空なら自身を利用")] 
    public Collider target;

    void Awake()
    {
        if (target == null)
        {
            target = GetComponent<Collider>();
        }

        if (target != null)
        {
            ApplyIce(target);
        }
    }

    public static PhysicsMaterial GetIceMaterial()
    {
        if (iceMaterial == null)
        {
            iceMaterial = new PhysicsMaterial("PM_Ice")
            {
                dynamicFriction = 0.1f,
                staticFriction = 0.1f,
                frictionCombine = PhysicsMaterialCombine.Minimum,
                bounceCombine = PhysicsMaterialCombine.Minimum,
                bounciness = 0f
            };
        }

        return iceMaterial;
    }

    public static void ApplyIce(Collider collider)
    {
        if (collider == null)
        {
            return;
        }

        collider.material = GetIceMaterial();
    }
}
