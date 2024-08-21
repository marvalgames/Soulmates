using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

public class SceneObjectsManager : MonoBehaviour
{
    [FormerlySerializedAs("activaateSceneObjects")] public GameObject[] activateSceneObjects;
    public float activateDelay = 1;
    
    IEnumerator Wait(float time)
    {
        for (int i = 0; i < activateSceneObjects.Length; i++)
        {
            activateSceneObjects[i].SetActive(false);
        }
        
        yield return new WaitForSeconds(time);

        for (int i = 0; i < activateSceneObjects.Length; i++)
        {
            activateSceneObjects[i].SetActive(true);
        }

        //GameInterface.instance.Paused = false;


    }
    void Start()
    {
  
        StartCoroutine(Wait(activateDelay));
        
    }

   

   
}
