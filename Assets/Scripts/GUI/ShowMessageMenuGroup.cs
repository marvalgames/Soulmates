using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using TMPro;
using Unity.Entities;


public struct ShowMessageMenuComponent : IComponentData
{
    public bool show;
}


public class ShowMessageMenuGroup : MonoBehaviour
{
    private EntityManager manager;
    public Entity entity;
    public AudioSource audioSource;
    private List<Button> buttons;
    //public AudioClip clickSound;
    public EventSystem eventSystem;
    private CanvasGroup canvasGroup;
    [SerializeField]
    private Button defaultButton;
    public float showTimeLength = 2.1f;
    private float showTimer = 0f;
    bool startShowTimer;
    [SerializeField] private TextMeshProUGUI message;
    public string messageString;
    [HideInInspector]
    public bool showOnce;
    public AudioClip voiceClip1;
    public AudioClip voiceClip2;
    public AudioClip voiceClip3;




    private void OnEnable()
    {
        //LevelOpen.showMessage += SetupMessage;

    }


    private void OnDisable()
    {
        //LevelOpen.showMessage -= SetupMessage;
    }


    void Start()
    {
        manager = World.DefaultGameObjectInjectionWorld.EntityManager;
        entity = World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntity();
        manager.AddComponentData(entity, new ShowMessageMenuComponent()
        {
            
        });
        manager.AddComponentObject(entity, this);

        audioSource = GetComponent<AudioSource>();
        canvasGroup = GetComponent<CanvasGroup>();
        startShowTimer = true;

    }


    private void SetupMessage(string _message)
    {
        messageString = _message;
        ShowMenu();
    }

    void Update()
    {
        if (manager == default) return;

        if (startShowTimer)
        {
            showTimer += Time.deltaTime;
            if (showTimer > showTimeLength)
            {
                showTimer = 0;
                startShowTimer = false;
                showOnce = false;
                HideMenu();
            }
        }
    }




    public void ShowMenu()
    {
        message.text = messageString;  
        startShowTimer = true;
        canvasGroup.alpha = 1;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        if (defaultButton)
        {
            defaultButton.Select();
        }
    }

    public void HideMenu()
    {
        canvasGroup.interactable = false;
        canvasGroup.alpha = 0.0f;
        canvasGroup.blocksRaycasts = false;

    }

   
   
}

