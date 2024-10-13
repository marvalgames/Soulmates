using UnityEngine;

////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.Hybrid
{
public class FABRIKAuthoring: MonoBehaviour
{
    [Range(0, 1)]
    public float weight = 1;
    public Transform tip;
    public Transform target;
    public int numIterations = 15;
    public float threshold = 0.00001f;

    void OnEnable() { }
}
}
