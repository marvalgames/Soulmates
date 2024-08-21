using UnityEngine;

public class SimpleAttachCamera : MonoBehaviour
{
    public Transform AttachTo;
    public Vector3 Offset;

    void LateUpdate()
    {
        transform.position = AttachTo.position + AttachTo.TransformDirection(Offset);
    }
}
