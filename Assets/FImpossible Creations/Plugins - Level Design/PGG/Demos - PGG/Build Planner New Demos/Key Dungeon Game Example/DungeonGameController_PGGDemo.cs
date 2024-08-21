using FIMSpace.Generating;
using System.Collections;
using UnityEngine;

public class DungeonGameController_PGGDemo : MonoBehaviour
{
    public static DungeonGameController_PGGDemo Instance { get; private set; }

    public BuildPlannerExecutor LevelGenerator;
    public GameObject PlayerObject;

    [Header("Gameplay Elements (Auto getted on start)")]
    public SimpleGate BossKeyGate;
    public SimpleGate FinishLevelGate;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        StartCoroutine(NextLevel(LevelGenerator.Generated.Count == 0));
    }

    public void StepToNextLevel()
    {
        if (!generatingLevel) StartCoroutine(NextLevel(true));
    }

    bool generatingLevel = false;
    private IEnumerator NextLevel(bool regenerate)
    {
        PlayerObject.GetComponent<Rigidbody>().useGravity = false;

        generatingLevel = true;
        yield return new WaitForSeconds(0.25f);

        if (regenerate)
        {
            LevelGenerator.ClearGenerated();

            LevelGenerator.transform.position = PlayerObject.transform.position;
            LevelGenerator.transform.position = new Vector3(LevelGenerator.transform.position.x, 0, LevelGenerator.transform.position.z);

            LevelGenerator.Seed += 1;

            LevelGenerator.Generate();
            Time.timeScale = 0.001f;
            yield return new WaitForSecondsRealtime(1f);
        }

        while (LevelGenerator.IsGenerating) yield return null;
        Time.timeScale = 1f;

        OnLevelStartReady();
        PlayerObject.GetComponent<Rigidbody>().useGravity = true;

        yield return new WaitForSecondsRealtime(1f);
        generatingLevel = false;
    }

    public void OnLevelStartReady()
    {
        GridPainter bossField = LevelGenerator.GetGeneratedGenerator("BossRoom") as GridPainter;
        BossKeyGate = bossField.GetComponentFromAllGenerated<SimpleGate>();
        
        GridPainter corridorField = LevelGenerator.GetGeneratedGenerator("Corridors") as GridPainter;
        FinishLevelGate = corridorField.GetComponentFromAllGenerated<SimpleGate>();
    }

    public void OnKeyCollected()
    {
        BossKeyGate.OpenWithCamera();
    }

    public void OnBossDeath()
    {
        FinishLevelGate.OpenWithCamera();
    }
}
