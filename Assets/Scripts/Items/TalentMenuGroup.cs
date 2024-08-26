using System;
using System.Collections.Generic;
using System.Linq;
using Rewired;
using Sandbox.Player;
using TMPro;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public struct TalentMenuComponent : IComponentData
{
    public bool showMenu;
    public bool exitClicked;
    public bool menuStateChanged;
    public int usedItem;
}

[Serializable]
public class MenuTalentItemData
{

    //public int[] ItemIndex = new int[65];
    //public int[] SlotUsed = new int[65];
    //public int[] UseSlot = new int[4];//use buttons 1-4
    //public Entity[] ItemEntity = new Entity[65];
    //public int CurrentIndex;
    //public int Count;
    //public int Remain;//how many still available left to choose from pick up list
    public Image Image;

}

[Serializable]
public class TalentItemData
{
    public TextMeshProUGUI longDescriptionlabel;//make class member with all attributes of Resource list 

}

public class TalentMenuGroup : MonoBehaviour
{

    //public static event Action<bool> HideSubscriberMenu;
    public bool useUpdated;
    public int playerIndex;

    private EntityManager manager;
    public Entity entity;
    public List<TalentItemComponent> talentItemComponents = new List<TalentItemComponent>();

    public List<TalentItemComponent> passedTalentItemComponents = new List<TalentItemComponent>();

    //public static ResourceItemComponent[] useItemComponents = new ResourceItemComponent[2];

    AudioSource audioSource;
    [SerializeField]
    private List<Button> buttons;
    [SerializeField] private Button exitButton;
    public AudioClip clickSound;
    public EventSystem eventSystem;
    private CanvasGroup canvasGroup;

    [SerializeField]
    TalentItemData TalentItemData;
    [SerializeField]
    private TextMeshProUGUI[] Talentlabel;

    [SerializeField] private TextMeshProUGUI[] selectedItemStatDescription;
    [SerializeField] private TextMeshProUGUI[] selectedItemStatRating;
    [SerializeField] private TextMeshProUGUI[] selectedItemStatDescriptionLong;
    [SerializeField] private Image statsBkImage;
    [SerializeField] private Sprite defaultSprite;
    [SerializeField] private Button defaultBtn;

    [SerializeField]
    private int buttonClickedIndex;
    public static bool UpdateMenu = true;
    [NonReorderable]
    public MenuTalentItemData[] menuTalentItem = new MenuTalentItemData[17];//10 items (buttons) max currently

    int selectedTalent;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    void Init()
    {
        passedTalentItemComponents.Clear();
    }
    void Start()
    {
        //string test = "dd";
        if (!ReInput.isReady) return;
        var world = World.DefaultGameObjectInjectionWorld;
        manager = world.EntityManager;
        entity = manager.CreateEntity();
        manager.AddComponentObject(entity, this);
        //manager.AddComponentData(entity, new TalentItemComponent());
        manager.AddComponentData(entity, new TalentMenuComponent());

        //player = ReInput.players.GetPlayer(0);
        audioSource = GetComponent<AudioSource>();
        canvasGroup = GetComponent<CanvasGroup>();
        AddMenuButtonHandlers();
        ShowLabels();
        ClearStatRows();

    }

    private void OnEnable()
    {
        PickupMenuGroup.HideSubscriberMenu += HideSubscriberMenu;
    }
    private void OnDisable()
    {
        PickupMenuGroup.HideSubscriberMenu -= HideSubscriberMenu;
    }

    private void HideSubscriberMenu(bool resume)
    {
        HideMenu();//event has parameter but this has HideMenu method with no parameter so just call from here
                   //could have used ontabclicked - has parameter

    }





    public void Count()
    {

        var tempItems = new List<TalentItemComponent>(passedTalentItemComponents);
        talentItemComponents.Clear();
        //var TalentUps = 16;//change this as we add new TalentUps
        //Debug.Log("items " + tempItems.Count);
        for (var j = 0; j < tempItems.Count; j++)//probably not needed - just copying j for j
        {
            var menu = menuTalentItem[j];
            var ico = menu.Image;
            var item = tempItems[j];
            if (item.itemPickedUp)
            {
                item.menuIndex = j;
                tempItems[j] = item;
                talentItemComponents.Add(tempItems[j]);
                menu.Image = ico;
                menuTalentItem[j] = menu;
            }
        }

        ShowLabels();

    }


    public void ShowLabels()
    {



        for (var i = 0; i < Talentlabel.Length; i++)
        {
            Talentlabel[i].text = "";
        }


        for (var i = 0; i < Talentlabel.Length; i++)
        {

            if (i < talentItemComponents.Count)
            {
                Talentlabel[i].text = talentItemComponents[i].description.ToString();
                var index = talentItemComponents[i].menuIndex;
                buttons[i + 1].interactable = true;
                Debug.Log("t1");
            }
            //buttons[i + 1].interactable = false;
        }




        GameLabels();





    }

    public void GameLabels()
    {



    }

    void ButtonClickedIndex(int index)
    {
        buttonClickedIndex = index;
        Debug.Log("btn idx " + buttonClickedIndex);

    }




    private void AddMenuButtonHandlers()
    {
        buttons = GetComponentsInChildren<Button>().ToList();//linq using

        buttons.ForEach(btn => btn.onClick.AddListener(() =>
            PlayMenuClickSound(clickSound)));//shortcut instead of using inspector to add to each button

        for (var i = 0; i < buttons.Count; i++)
        {
            var temp = i;
            buttons[i].onClick.AddListener(() => { ButtonClickedIndex(temp); });
        }

        exitButton.onClick.AddListener(ExitClicked);


    }

    void QueryEntities()
    {
        var itemQuery = manager.CreateEntityQuery(ComponentType.ReadOnly<TalentItemComponent>());
        var itemGroup = itemQuery.ToComponentDataArray<TalentItemComponent>(Allocator.Persistent);

        var playerQuery = manager.CreateEntityQuery(ComponentType.ReadOnly<PlayerComponent>());
        var playerEntities = playerQuery.ToEntityArray(Allocator.TempJob);


        var powerItems = new List<TalentItemComponent>();

        for (var i = 0; i < itemGroup.Length; i++)
        {
            if (itemGroup[i].itemPickedUp && itemGroup[i].pickedUpActor == playerEntities[playerIndex])
            {
                powerItems.Add(itemGroup[i]);
                Debug.Log("power " + itemGroup[i].statDescription1);
            }
        }

        playerEntities.Dispose();
        passedTalentItemComponents = powerItems;
    }

    public void SelectedTalent(int index)
    {
        ClearStatRows();
        TalentItemData.longDescriptionlabel.text = "";
        statsBkImage.sprite = menuTalentItem[index].Image.sprite;

        selectedTalent = index;
        //if (menuTalentItem.Length == 0) return; 
        if (talentItemComponents.Count <= selectedTalent) return;


        var TalentUp = talentItemComponents[selectedTalent];
        TalentItemData.longDescriptionlabel.text = TalentUp.longDescription.ToString();
        Debug.Log("sel " + selectedTalent);
        //int co = TalentUp.statCount;

        for (var j = 0; j < 3; j++)
        {
            if (j == 0 && TalentUp.statRating1 != 0)
            {
                selectedItemStatDescription[j].text = TalentUp.statDescription1.ToString();
                selectedItemStatRating[j].text = TalentUp.statRating1.ToString();
                selectedItemStatDescriptionLong[j].text = TalentUp.statDescriptionLong1.ToString();
            }
            else if (j == 1 && TalentUp.statRating2 != 0)
            {
                selectedItemStatDescription[j].text = TalentUp.statDescription2.ToString();
                selectedItemStatRating[j].text = TalentUp.statRating2.ToString();
                selectedItemStatDescriptionLong[j].text = TalentUp.statDescriptionLong2.ToString();
            }
            else if (j == 2 && TalentUp.statRating3 != 0)
            {
                selectedItemStatDescription[j].text = TalentUp.statDescription3.ToString();
                selectedItemStatRating[j].text = TalentUp.statRating3.ToString();
                selectedItemStatDescriptionLong[j].text = TalentUp.statDescriptionLong3.ToString();
            }


        }

        Count();
        ShowLabels();

    }

    private void ClearStatRows()
    {
        var rowsToClear = 3; //holder 3 stats max for now
        for (var i = 0; i < rowsToClear; i++)
        {
            selectedItemStatDescription[i].text = "";
            selectedItemStatRating[i].text = "";
            selectedItemStatDescriptionLong[i].text = "";
        }

    }


    private void ExitClicked()
    {


    }

    public void OnTalentTabClicked(bool show)
    {
        canvasGroup.interactable = show;
        canvasGroup.blocksRaycasts = show;
        canvasGroup.alpha = show ? 1 : 0;
        QueryEntities();
        //if (show) 
        Count();
        if (defaultBtn == null || !show) return;
        defaultBtn.Select();
    }


    public void ShowMenu()
    {
        QueryEntities();
        Count();
        ShowLabels();

    }

    public void HideMenu()
    {
        //PauseGame?.Invoke(false);
        //GameInterface.Paused = false;
        //GameInterface.StateChange = true;
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


[UpdateInGroup(typeof(SimulationSystemGroup))]
[RequireMatchingQueriesForUpdate]

public partial class TalentSystem : SystemBase
{

    protected override void OnUpdate()
    {

    }


}