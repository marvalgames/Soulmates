using UnityEngine;

////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.Hybrid
{
public class OverrideTransformIKAuthoring: MonoBehaviour
{
    public Transform target;
    [Range(0, 1)]
    public float positionWeight = 1;
    [Range(0, 1)]
    public float rotationWeight = 1;
    
////////////////////////////////////////////////////////////////////////////////////////

    void OnEnable() { }
}
}
