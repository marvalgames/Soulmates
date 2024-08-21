using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.Player;
using TMPro;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public struct SkillTreeMenuComponent : IComponentData
{
    public bool showMenu;
    public bool exitClicked;
    public bool menuStateChanged;
}



public class SkillTreeMenuGroup : MonoBehaviour
{

    public static event Action<bool> PauseGame;

    private EntityManager manager;
    public Entity entity;
    public List<SkillTreeComponent> playerSkillSets = new List<SkillTreeComponent>();
    public SkillTreeComponent player0_skillSet;

    AudioSource audioSource;
    private List<Button> buttons;
    [SerializeField] private Button exitButton;
    public AudioClip clickSound;
    public EventSystem eventSystem;
    private CanvasGroup canvasGroup;

    [SerializeField]
    private TextMeshProUGUI label0;

    public int speedPts;
    public int powerPts;
    public int chinPts;
    public int availablePoints;

    private int buttonClickedIndex;


    void Start()
    {
        var manager = World.DefaultGameObjectInjectionWorld.EntityManager;
        var entity = World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntity();
        manager.AddComponentData(entity, new SkillTreeMenuComponent()
        {
            
        });
        manager.AddComponentObject(entity, this);
        
        audioSource = GetComponent<AudioSource>();
        canvasGroup = GetComponent<CanvasGroup>();
        AddMenuButtonHandlers();
    }



    void Update()
    {

        if (entity == Entity.Null || manager == default) return;
        var hasComponent = manager.HasComponent(entity, typeof(SkillTreeMenuComponent));
        if (hasComponent == false) return;

        //move to system update below
        var stateChange = manager.GetComponentData<SkillTreeMenuComponent>(entity).menuStateChanged;

        if (stateChange == true)
        {
            var skillTreeMenu = manager.GetComponentData<SkillTreeMenuComponent>(entity);
            skillTreeMenu.menuStateChanged = false;
            manager.SetComponentData(entity, skillTreeMenu);

            if (manager.GetComponentData<SkillTreeMenuComponent>(entity).showMenu)
            {
                ShowMenu();
            }
            else
            {
                HideMenu();
            }
            EnableButtons();
        }
        ShowLabel0();
    }

    void EnableButtons()
    {
        exitButton.Select();

        buttons[1].interactable = false;
        buttons[2].interactable = false;
        buttons[3].interactable = false;
        if (availablePoints >= 1 && speedPts == 0)
        {
            buttons[1].interactable = true;
            buttons[1].Select();
        }
        else if (availablePoints >= 2 && speedPts == 1)
        {
            buttons[2].interactable = true;
            buttons[2].Select();
        }
        else if (availablePoints >= 3 && speedPts == 2)
        {
            buttons[3].interactable = true;
            buttons[3].Select();
        }


    }

    void ShowLabel0()
    {
        label0.text = "Points : " + availablePoints;
    }

    void ButtonClickedIndex(int index)
    {
        buttonClickedIndex = index;
        Debug.Log("btn idx " + buttonClickedIndex);
        if (index >= 1 && index <= 3)
        {
            if (manager.HasComponent<SkillTreeComponent>(entity) == false) return;
            availablePoints = availablePoints - index;
            speedPts = index;
            player0_skillSet.availablePoints = availablePoints;
            player0_skillSet.SpeedPts = speedPts;
            EnableButtons();

        }
    }



    public void InitCurrentPlayerSkillSet()
    {
        if (manager.HasComponent<SkillTreeComponent>(entity) == false) return;
        player0_skillSet = manager.GetComponentData<SkillTreeComponent>(entity);

        speedPts = manager.GetComponentData<SkillTreeComponent>(entity).SpeedPts;
        powerPts = manager.GetComponentData<SkillTreeComponent>(entity).PowerPts;
        chinPts = manager.GetComponentData<SkillTreeComponent>(entity).ChinPts;
        availablePoints = manager.GetComponentData<SkillTreeComponent>(entity).availablePoints;
        var e = manager.GetComponentData<SkillTreeComponent>(entity).e;

        if (playerSkillSets.Count < 1) //1 for now
        {
            playerSkillSets.Add(player0_skillSet);
        }

    }



    public void WriteCurrentPlayerSkillSet()
    {
        if (manager.HasComponent<SkillTreeComponent>(entity) == false) return;
        var skillTree = player0_skillSet;
        var playerE = skillTree.e;
        manager.SetComponentData(playerE, player0_skillSet);

    }


    private void AddMenuButtonHandlers()
    {
        buttons = GetComponentsInChildren<Button>().ToList();//linq using

        buttons.ForEach((btn) => btn.onClick.AddListener(() =>
            PlayMenuClickSound(clickSound)));//shortcut instead of using inspector to add to each button

        for (var i = 0; i < buttons.Count; i++)
        {
            var temp = i;
            buttons[i].onClick.AddListener(() => { ButtonClickedIndex(temp); });
        }

        exitButton.onClick.AddListener(ExitClicked);


    }

    private void ExitClicked()
    {
        WriteCurrentPlayerSkillSet();
        var skillTreeMenu = manager.GetComponentData<SkillTreeMenuComponent>(entity);
        skillTreeMenu.exitClicked = true;
        manager.SetComponentData(entity, skillTreeMenu);

    }


    public void ShowMenu()
    {
        InitCurrentPlayerSkillSet();
        PauseGame?.Invoke(true);
        canvasGroup.alpha = 1;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

    public void HideMenu()
    {
        PauseGame?.Invoke(false);
        canvasGroup.interactable = false;
        canvasGroup.alpha = 0.0f;
        canvasGroup.blocksRaycasts = false;

    }




    void PlayMenuClickSound(AudioClip clip)
    {
        audioSource.PlayOneShot(clip);
        Debug.Log("clip " + clip);


    }

   
}

