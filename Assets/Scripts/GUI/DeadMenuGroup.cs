using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Linq;
using System.Collections.Generic;
using Unity.Entities;
using TMPro;

[System.Serializable]
public struct DeadMenuComponent : IComponentData
{
    public bool hide;
}


public class DeadMenuGroup : MonoBehaviour      
{

    // Use this for initialization
    public AudioSource audioSource;
    private List<Button> buttons;
    public AudioClip clickSound;
    public EventSystem eventSystem;
    private CanvasGroup canvasGroup;
    [SerializeField]
    private Button defaultButton;

    [SerializeField] private ParticleSystem deadParticleSystem;
    [SerializeField]
    private TextMeshProUGUI message;
    [SerializeField] private float showTimer = 3;


    [HideInInspector] public bool showMenu;
    [HideInInspector] public bool showScoreboard;
    [HideInInspector] public int score;
    [HideInInspector] public int rank;

    void Start()
    {   var world = World.DefaultGameObjectInjectionWorld;
        var manager = world.EntityManager;
        var entity = manager.CreateEntity();
        manager.AddComponentData(entity, new DeadMenuComponent() {hide = true});
        manager.AddComponentObject(entity, this);
        audioSource = GetComponent<AudioSource>();
        canvasGroup = GetComponent<CanvasGroup>();
        buttons = gameObject.GetComponentsInChildren<Button>().ToList();
        buttons.ForEach((btn) => btn.onClick.AddListener(() =>
        PlayMenuClickSound(clickSound)));//shortcut instead of using inspector to add to each button

    }

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


        GameInterface.instance.Paused = true;
        GameInterface.instance.EnableControllerMaps(false, true, false);

        canvasGroup.alpha = 1;

        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        if (defaultButton)
        {
            defaultButton.Select();
            Debug.Log("Select " + defaultButton);

        }

        if (deadParticleSystem)
        {
            deadParticleSystem.Play(true);
        }

        if (audioSource)
        {
            audioSource.Play();
        }


        if (showScoreboard == false)
        {
            message.SetText("Game Over");
        }
        else
        {
            message.SetText("SCORE: " + score + " RANK:  " + rank);
        }



    }




    public void HideMenu()
    {
        GameInterface.instance.EnableControllerMaps(true, false, false);

        canvasGroup.interactable = false;
        canvasGroup.alpha = 0.0f;
        canvasGroup.blocksRaycasts = false;

    }


    void PlayMenuClickSound(AudioClip clip)
    {
        audioSource.PlayOneShot(clip);

    }


}

