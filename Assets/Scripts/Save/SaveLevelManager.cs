using System.Collections.Generic;
using UnityEngine;




public class SaveLevelManager : MonoBehaviour
{

    public bool saveScene = false;
    public bool loadNextScene = false;
    public bool levelMenuShown = false;

    public List<SaveLevelPlayers> saveLevelPlayers = new List<SaveLevelPlayers>();


    public static SaveLevelManager instance = null;


    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
            Destroy(gameObject);


    }



}











