using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PGGDemo_Rotator : MonoBehaviour
{
    public Vector3 RotatePerFrame = new Vector3(0, 1, 0);

    void Update()
    {
        transform.Rotate(RotatePerFrame * Time.deltaTime * 60f, Space.Self);    
    }
}
