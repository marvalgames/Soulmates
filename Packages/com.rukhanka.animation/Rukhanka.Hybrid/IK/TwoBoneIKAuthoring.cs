using UnityEngine;

////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.Hybrid
{
public class TwoBoneIKAuthoring: MonoBehaviour
{
    [Range(0, 1)]
    public float weight = 1;
    public Transform mid;
    public Transform tip;
    public Transform target;
    public Transform midBentHint;

    void OnEnable() { }
}
}
