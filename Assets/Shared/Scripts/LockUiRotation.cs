using UnityEngine;
using System.Collections;

public class LockUiRotation : MonoBehaviour {


    // Update is called once per frame
    void Update () {

        transform.rotation = Camera.main.transform.rotation;


    }
}
