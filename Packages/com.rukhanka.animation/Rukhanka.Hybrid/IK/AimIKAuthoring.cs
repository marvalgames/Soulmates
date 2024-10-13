using UnityEngine;

////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.Hybrid
{
public class AimIKAuthoring: MonoBehaviour
{
    public Transform target;
    public float angleLimitMin;
    public float angleLimitMax;
    public Vector3 forwardVector = Vector3.forward;
    [Range(0, 1)]
    public float weight = 1;

    public WeightedTransform[] affectedBones;
    
////////////////////////////////////////////////////////////////////////////////////////

    void OnEnable() { }
}
}
