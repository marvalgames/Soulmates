using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;




public class SaveManager : MonoBehaviour
{

    public static SaveManager instance = null;

    public SaveData saveData = new SaveData();
    public SaveWorld saveWorld = new SaveWorld();

    public bool updateScore = false;
    public bool saveMainGame = false;



    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
            Destroy(gameObject);

        saveWorld = LoadSaveWorld();
        saveData = LoadSaveData();
        //SaveManager.instance.SaveGameData();
        DontDestroyOnLoad(gameObject);


    }


    void Start()
    {

    }

    public void SaveCurrentLevelCompleted(int level)
    {
        if (saveData.saveGames.Count == 0)
        {
            saveData.saveGames.Add(new SaveGames());
        }
        saveData.saveGames[0].currentLevel = level;
    }

    public SaveWorld LoadSaveWorld()
    {
        //Debug.Log("load Save world");
        SaveWorld sw;
        var path = Application.persistentDataPath + "/game.wor";
        Debug.Log("Path " + path);
        if (File.Exists(path))
        {
            var bf = new BinaryFormatter();
            var file = File.Open(path, FileMode.Open);
            sw = bf.Deserialize(file) as SaveWorld;
            Debug.Log("loading " + path);
            file.Close();
        }
        else
        {
            var bf = new BinaryFormatter();
            var file = File.Create(path);
            sw = new SaveWorld {  };
            Debug.Log("creating " + path);
            bf.Serialize(file, sw);
            file.Close();
        }

        //Debug.Log("save world load res height "+saveWorld.screenResHeight);

        return sw;

    }

    

    public SaveData LoadSaveData()
    {
        //int slot = saveWorld.lastLoadedSlot;
        //int slot = 0;
        var sd = new SaveData();
        //string path = Application.persistentDataPath + "/savedGames" + slot.ToString() + ".sog";
        var path = Application.persistentDataPath + "/savedGames" + ".sog";
        if (File.Exists(path))
        {
            //Debug.Log(path + " exists");
            var bf = new BinaryFormatter();

            try
            {
                var file = File.Open(path, FileMode.Open);
                sd = bf.Deserialize(file) as SaveData;
                file.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine("user exception " + e);
                sd = new SaveData();
                //file.Close();
            }
        }
        else
        {
            Debug.Log(path + " created");
            var bf = new BinaryFormatter();
            var file = File.Create(path);
            sd = new SaveData();
            bf.Serialize(file, sd);
            file.Close();
        }


        if (sd.saveGames.Count == 0)
        {
            sd.saveGames.Add(new SaveGames());
            //sd.saveGames.Add(new SaveGames());
            //sd.saveGames.Add(new SaveGames());
        }

        return sd;
    }


    public void SaveGameData()
    {
        var bf = new BinaryFormatter();
        //string fileName = Application.persistentDataPath + "/savedGames" + (saveWorld.lastLoadedSlot).ToString() + ".sog";
        var fileName = Application.persistentDataPath + "/savedGames" + ".sog";
        var file = File.Create(fileName);
        Debug.Log(Application.persistentDataPath);

        try
        {
            bf.Serialize(file, saveData);
        }
        catch (Exception e)
        {
            Console.WriteLine("save game data " + e);
        }
        file.Close();
    }


    public void SaveWorldSettings()
    {
        Debug.Log("save world");
        //int slot = saveWorld.lastLoadedSlot;
        //saveWorld.isSlotSaved[slot] = true && slot > 0; 
        var path = Application.persistentDataPath + "/game.wor";
        var bf = new BinaryFormatter();
        var file = File.Create(path);
        bf.Serialize(file, saveWorld);
        file.Close();

    }




    public void DeleteGameData(int slot)//always 1 now
    {
        saveWorld.isSlotSaved[slot-1] = false;
     
        var count = saveData.saveGames.Count;
        if (slot == 1 && count == 0)
        {
            saveData.saveGames.Add(new SaveGames());
        }

        var scoreList = saveData.saveGames[slot - 1].scoreList;
        saveData.saveGames[slot - 1] = new SaveGames {scoreList = scoreList};

        SaveGameData();

    }





    public void DeleteHighScoreData()
    {
        //int slot = saveWorld.lastLoadedSlot - 1;
        var slot = 0;
        saveData.saveGames[slot].scoreList.Clear();
        Debug.Log("clear");
        SaveGameData();

    }

}











