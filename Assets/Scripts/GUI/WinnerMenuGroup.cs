using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Linq;
using System.Collections.Generic;
using TMPro;
using Unity.Entities;


public enum toggleStates
{
    off,
    on,
    post
}

[System.Serializable]
public struct WinnerMenuComponent : IComponentData
{
    public bool hide;
    public int levelTargetReachedCounter;
    public int endGameTargetReachedCounter;
    public int npcDeadCounter;
    public int score;
    public int rank;
    public bool showScore;
    //public bool scoreBoard;


}

[System.Serializable]
public struct WinnerComponent : IComponentData
{
    public bool active;
    public int goalCounter;
    public int goalCounterTarget;
    public bool targetReached;
    public bool endGameReached;
    public bool resetLevel;
    public bool checkWinCondition;
    public int winnerCounter;
    public int keys;
    public toggleStates winConditionMet;



}

public class WinnerMenuGroup : MonoBehaviour
{
    [HideInInspector]
    public AudioSource audioSource;
    private List<Button> buttons;
    public AudioClip clickSound;
    public EventSystem eventSystem;
    private CanvasGroup canvasGroup;
    [SerializeField]
    private Button defaultButton;
    [SerializeField]
    private TextMeshProUGUI message;
    [SerializeField] private float showTimer = 3;


    [SerializeField] private ParticleSystem winnerParticleSystem;

    [HideInInspector] public bool showMenu;
    [HideInInspector] public bool showScoreboard;
    [HideInInspector] public int score;
    [HideInInspector] public int rank;


    void OnEnable()
    {
        ScoreMenuGroup.ScoreMenuExitBackClickedEvent += ResetSelectedButton;
    }

    void OnDisable()
    {
        ScoreMenuGroup.ScoreMenuExitBackClickedEvent -= ResetSelectedButton;
    }


    private void ResetSelectedButton()
    {
        //EventSystem.current.SetSelectedGameObject(defaultButton.gameObject);
        if (canvasGroup.interactable)
        {
            defaultButton.Select(); //not working
            Debug.Log("Select " + defaultButton);
        }
    }


    void Start()
    {
        
        var world = World.DefaultGameObjectInjectionWorld;
        var manager = world.EntityManager;
        var entity = manager.CreateEntity();
        manager.AddComponentData(entity, new WinnerMenuComponent()
        {
            hide = true,
            levelTargetReachedCounter = 0,
            endGameTargetReachedCounter = 0
        });
        manager.AddComponentObject(entity, this);

        
        audioSource = GetComponent<AudioSource>();
        canvasGroup = GetComponent<CanvasGroup>();
        buttons = gameObject.GetComponentsInChildren<Button>().ToList();
        buttons.ForEach((btn) => btn.onClick.AddListener(() =>
        PlayMenuClickSound(clickSound)));//shortcut instead of using inspector to add to each button

    }


    void Update()
    {
        if (showMenu && showTimer >= 0)
        {
            showTimer -= Time.deltaTime;
            if (showTimer <= 0)
            {
                ShowMenu();
            }
        }
    }


    public void ShowMenu()
    {
        canvasGroup.alpha = 1;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        GameInterface.instance.Paused = true;
        GameInterface.instance.EnableControllerMaps(false,true,false);
        if (defaultButton)
        {
            defaultButton.Select();
        }

        if (winnerParticleSystem)
        {
            winnerParticleSystem.Play(true);
        }

        if (audioSource)
        {
            audioSource.Play();
        }


        if (showScoreboard == false)
        {
            message.SetText("Winner!");
        }
        else
        {
            message.SetText("SCORE: " + score + " RANK:  " + rank);
        }



    }

    public void HideMenu()
    {
        canvasGroup.interactable = false;
        canvasGroup.alpha = 0.0f;
        canvasGroup.blocksRaycasts = false;

    }


    void PlayMenuClickSound(AudioClip clip)
    {
        audioSource.PlayOneShot(clip);

    }

    
}

