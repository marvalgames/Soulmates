using System.Collections.Generic;
using UnityEngine;

public class SaveLevelManager : MonoBehaviour
{

    public bool saveScene;
    public bool loadNextScene;
    public bool levelMenuShown;

    public List<SaveLevelPlayers> saveLevelPlayers = new List<SaveLevelPlayers>();


    public static SaveLevelManager instance;


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











