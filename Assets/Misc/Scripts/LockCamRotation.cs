using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class LockCamRotation : MonoBehaviour
{
    [SerializeField] private float slerpTime = .12f;
    void Update()
    {
        transform.rotation = Quaternion.Slerp(transform.rotation,  Quaternion.identity, slerpTime);
    }


    
}


