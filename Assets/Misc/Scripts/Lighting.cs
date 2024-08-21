using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lighting : MonoBehaviour
{
    public Light directionalLight;
    public float intensity;
    public Color lightColor;

    // Start is called before the first frame update
    void Start()
    {
        UpdateLights();
    }

    // Update is called once per frame

    public void UpdateLights()
    {
        directionalLight.intensity = intensity;
        directionalLight.color = lightColor;
    }
}
