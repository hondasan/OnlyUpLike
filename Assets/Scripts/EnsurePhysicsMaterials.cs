using UnityEngine;

public class EnsurePhysicsMaterials : MonoBehaviour
{
    static PhysicMaterial iceMaterial;

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

    public static PhysicMaterial GetIceMaterial()
    {
        if (iceMaterial == null)
        {
            iceMaterial = new PhysicMaterial("PM_Ice")
            {
                dynamicFriction = 0.1f,
                staticFriction = 0.1f,
                frictionCombine = PhysicMaterialCombine.Minimum,
                bounceCombine = PhysicMaterialCombine.Minimum,
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
