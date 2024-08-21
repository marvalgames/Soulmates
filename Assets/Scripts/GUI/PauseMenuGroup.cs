using System;
using System.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;



public class PauseMenuGroup : MonoBehaviour
{

    public delegate void ActionResume();
    public static event ActionResume ResumeClickedEvent;//gameinterface subscribes to this

    //public static event Action OptionsClickedEvent;//this is same as two lines above - action keyword shorthand
    public static event Action SaveExitClickedEvent;
    public static event Action ExitClickedEvent;
    public static event Action ScoresClickedEvent;

    private CanvasGroup canvasGroup = null;
    [SerializeField]
    private DeadMenuGroup deadGroup = null;
    [SerializeField]
    private WinnerMenuGroup winnerGroup = null;
    [SerializeField]
    private EventSystem eventSystem;
    [SerializeField]
    private Button defaultButton;
    [SerializeField]
    private Button resumeButton;
    [SerializeField]
    private Button optionsButton;
    [SerializeField]
    private Button saveExitButton;
    [SerializeField]
    private Button exitButton;
    [SerializeField]
    private Button scoresButton;

    private EntityManager manager;
    private Entity e;

    //public static bool Paused;

    private void OnResumeClickedEvent()
    {
        //subscriber game interface
        ResumeClickedEvent?.Invoke();
    }

    //public static event Action SaveExitClickedEvent;

    private void OnOptionsClickedEvent()
    {
        //subscriber options menu group -> showmenu 
        //OptionsClickedEvent?.Invoke();
    }

    private void OnSaveExitClickedEvent()
    {
        //subscriber scene switcher  -> save and exit
        manager.SetComponentData(e, new SaveComponent { value = true });
        StartCoroutine(Wait(.19f));

    }

    private void OnExitClickedEvent()
    {
        ExitClickedEvent?.Invoke();
        //subscriber scene switcher  -> save and exit

    }

    private void OnScoresClickedEvent()
    {
        ScoresClickedEvent?.Invoke();
        //subscriber score menu group -> showmenu(false)

    }


    void Start()
    {
        
        canvasGroup = GetComponent<CanvasGroup>();
        resumeButton.onClick.AddListener(OnResumeClickedEvent);
        optionsButton.onClick.AddListener(OnOptionsClickedEvent);
        saveExitButton.onClick.AddListener(OnSaveExitClickedEvent);
        scoresButton.onClick.AddListener(OnScoresClickedEvent);
        exitButton.onClick.AddListener(OnExitClickedEvent);
        
        var world = World.DefaultGameObjectInjectionWorld;
        manager = world.EntityManager;
        var entity = manager.CreateEntity();
        manager.AddComponent<SaveComponent>(entity);
        manager.AddComponentObject(entity, this);
        e = entity;
    }

    private void OnEnable()
    {
        GameInterface.SelectClickedEvent += ShowMenu;
        SkillTreeMenuGroup.PauseGame += SkillTreeMenuPanel;
        ScoreMenuGroup.ScoreMenuExitBackClickedEvent += ResetSelectedButton;
        PickupMenuGroup.HideSubscriberMenu += HideMenu;
    }


    private void OnDisable()
    {
        GameInterface.SelectClickedEvent -= ShowMenu;
        SkillTreeMenuGroup.PauseGame -= SkillTreeMenuPanel;
        ScoreMenuGroup.ScoreMenuExitBackClickedEvent -= ResetSelectedButton;
        PickupMenuGroup.HideSubscriberMenu -= HideMenu;
    }


    private void ResetSelectedButton()
    {
        //EventSystem.current.SetSelectedGameObject(defaultButton.gameObject);
        if (canvasGroup.interactable)
        {
            defaultButton.Select(); //not working
            Debug.Log("Select " + defaultButton.gameObject);
        }
    }

    private void SkillTreeMenuPanel(bool paused)
    {
    }

    private void PauseMenuPanel(bool paused)
    {
    }

    public void HideMenu(bool resume)
    {
        if (resume)
        {
            GameInterface.instance.Paused = false;
            GameInterface.instance.StateChange = true;
        }
        canvasGroup.interactable = false;
        canvasGroup.alpha = 0.0f;
        canvasGroup.blocksRaycasts = false;
    }



    public void ShowMenu()//should have separated from HideMenu
    {
        //menuCanvas.GetComponent<Canvas>().gameObject.SetActive(show);
        //Canvas.ForceUpdateCanvases();
        //GameInterface.pauseMenuDisabled = false;
        if (deadGroup)
        {
            deadGroup.HideMenu();
        }
        else if (winnerGroup)
        {
            winnerGroup.HideMenu();
        }

        var show = GameInterface.instance.Paused && !GameInterface.instance.PauseMenuDisabled;//NA for now
        canvasGroup.alpha = show ? 1 : 0;
        canvasGroup.interactable = show;
        canvasGroup.blocksRaycasts = show;
        if (defaultButton && show)
        {
            Debug.Log("show " + defaultButton);
            defaultButton.Select();
            
        }
        
    }
    

    IEnumerator Wait(float time)
    {
        yield return new WaitForSeconds(time);
        SaveExitClickedEvent?.Invoke();
    }




}







