using UnityEngine;

public class SimpleIzoCamera : MonoBehaviour
{
    public static SimpleIzoCamera Instance { get; private set; }
    private void Awake()
    {
        Instance = this;
    }

    public Transform ToFollow;
    public float FollowDur = 0.3f;

    [Header("If zero then auto asfter start")]
    private Vector3 Offset = Vector3.zero;
    void Start()
    {
        Offset = transform.position - ToFollow.position;
    }

    Vector3 sm_tgtPos = Vector3.zero;
    void LateUpdate()
    {
        Vector3 targetPos = ToFollow.position + Offset;
        transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref sm_tgtPos, FollowDur, 1000f, Time.deltaTime);
    }
}
