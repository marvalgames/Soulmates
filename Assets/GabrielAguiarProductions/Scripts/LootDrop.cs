using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class LootDrop : MonoBehaviour
{
    public GameObject lootObject;
    public VisualEffect lootVFX;

    void Start()
    {
        lootVFX.Play();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnTriggerEnter(Collider co)
    {
        if(co.gameObject.tag == "Player")
        {
            lootVFX.Stop();
            lootObject.SetActive(false);
            Destroy (gameObject, 5);
        }

    }
}
