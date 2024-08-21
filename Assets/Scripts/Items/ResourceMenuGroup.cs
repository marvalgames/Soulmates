using Rewired;
using Sandbox.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;






public struct ResourceMenuComponent : IComponentData
{
    public bool showMenu;
    public bool exitClicked;
    public bool menuStateChanged;
    public int usedItem;
}

[Serializable]
public class MenuResourceItemData
{

    public int[] ItemIndex = new int[65];
    //public int[] SlotUsed = new int[65];
    public int[] UseSlot = new int[4];//use buttons 1-4
    public Entity[] ItemEntity = new Entity[65];
    public int CurrentIndex;
    public int Count;
    public int Remain;//how many still available left to choose from pick up list
    public Image Image;

}

[Serializable]
public class ResourceItemData
{
    public TextMeshProUGUI longDescriptionlabel;//make class member with all attributes of Resource list 

}

public class ResourceMenuGroup : MonoBehaviour
{

    //public static event Action<bool> HideSubscriberMenu;
    public static bool UseUpdated;

    private EntityManager _manager;
    public Entity _entity;
    public List<ResourceItemComponent> resourceItemComponents = new List<ResourceItemComponent>();

    public List<ResourceItemComponent> passedResourceItemComponents = new List<ResourceItemComponent>();
    AudioSource audioSource;
    [SerializeField]
    private List<Button> buttons;
    [SerializeField] private Button exitButton;
    public AudioClip clickSound;
    public EventSystem eventSystem;
    private CanvasGroup canvasGroup;

    [SerializeField]
    [NonReorderable]
    MenuResourceItemData[] menuResourceItem = new MenuResourceItemData[17];//10 items (buttons) max currently

    [SerializeField]
    ResourceItemData ResourceItemData;
    [SerializeField]
    private TextMeshProUGUI[] Resourcelabel;
    
    [SerializeField] private TextMeshProUGUI[] selectedItemStatDescription;
    [SerializeField] private TextMeshProUGUI[] selectedItemStatRating;
    [SerializeField] private TextMeshProUGUI[] selectedItemStatDescriptionLong;
    [SerializeField] private Image statsBkImage;
    [SerializeField] private Sprite defaultSprite;
    [SerializeField] private Button defaultBtn;

    
    [SerializeField]
    private int buttonClickedIndex;
    public static bool UpdateMenu = true;
   

    int selectedResource;

    void Init()
    {
        //Debug.Log("Counter reset.");
        passedResourceItemComponents.Clear();
    }
    void Start()
    {
        //string test = "dd";
        if (!ReInput.isReady) return;
        var world = World.DefaultGameObjectInjectionWorld;
        _manager = world.EntityManager;
        _entity = _manager.CreateEntity();
        _manager.AddComponent<ResourceMenuComponent>(_entity);
        _manager.AddComponentObject(_entity, this);
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


    public void UpdateItemEntities()
    {

        if (_entity == Entity.Null || _manager == default) return;



    }

    void Update()
    {
        UpdateItemEntities();
        var selected = eventSystem.currentSelectedGameObject;


    }




    
    public void EnableButtons()
    {


    }


    public void Count()
    {



        var tempItems = new List<ResourceItemComponent>(passedResourceItemComponents);
        resourceItemComponents.Clear();
        //var ResourceUps = 4;//change this as we add new Resourceups

        var first = -1;
        for (var j = 0; j < tempItems.Count; j++)
        {
            var menu = menuResourceItem[j];
            var ico = menu.Image;
            first = -1;
            var count = 0;
            menu.Count = count;
            for (var i = 0; i < tempItems.Count; i++)
            {
                menu.ItemIndex[i] = 0;
                menu.ItemEntity[i] = Entity.Null;
                if ((int)tempItems[i].resourceType == j + 1)
                {
                

                    Debug.Log("put " + tempItems[i].pickupEntity);
                    if (first == -1)
                    {
                        first = i;
                    }
                    menu.ItemEntity[count] = tempItems[i].pickupEntity;
                    menu.ItemIndex[count] = tempItems[i].pickupEntity.Index;
                    count += 1;
                    menu.Count = count;

                }

            }

            if (first >= 0)
            {
                var item = tempItems[first];
                item.count = count;
                item.menuIndex = j;
                tempItems[first] = item;
                resourceItemComponents.Add(tempItems[first]);
                
            }
            else
            {
                resourceItemComponents.Add(new ResourceItemComponent());
            }

           
            menu.Image = ico;
            menuResourceItem[j] = menu;


        }


        ShowLabels();


    }


    public void ShowLabels()
    {

       

        for (var i = 0; i < Resourcelabel.Length; i++)
        {
            Resourcelabel[i].text = "";
        }


        for (var i = 0; i < Resourcelabel.Length; i++)
        {

            if (i < resourceItemComponents.Count && resourceItemComponents[i].count > 0)
            {
                Resourcelabel[i].text = resourceItemComponents[i].description.ToString() + " " + resourceItemComponents[i].count;
                var index = resourceItemComponents[i].menuIndex;
                buttons[i + 1].interactable = true;
            }
            else
            {
                //buttons[i + 1].interactable = false;
            }
        }




        GameLabels();

        



    }

    public void GameLabels()
    {

  

    }

    void ButtonClickedIndex(int index)
    {
        buttonClickedIndex = index;
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

    public void SelectedResource(int index)
    {
        ClearStatRows();
        ResourceItemData.longDescriptionlabel.text = "";
        statsBkImage.sprite = menuResourceItem[index].Image.sprite;

        selectedResource = index;
        if (menuResourceItem[selectedResource].Count == 0) return;
        if (resourceItemComponents.Count <= selectedResource) return;
        var ResourceUp = resourceItemComponents[selectedResource];
        ResourceItemData.longDescriptionlabel.text = ResourceUp.longDescription.ToString();
        Debug.Log("sel " + selectedResource);

        for (var j = 0; j < 3; j++)
        {
            if (j == 0 && ResourceUp.statRating1 != 0)
            {
                selectedItemStatDescription[j].text = ResourceUp.statDescription1.ToString();
                selectedItemStatRating[j].text = ResourceUp.statRating1.ToString();
                selectedItemStatDescriptionLong[j].text = ResourceUp.statDescriptionLong1.ToString();
            }
            else  if (j == 1 && ResourceUp.statRating2 != 0)
            {
                selectedItemStatDescription[j].text = ResourceUp.statDescription2.ToString();
                selectedItemStatRating[j].text = ResourceUp.statRating2.ToString();
                selectedItemStatDescriptionLong[j].text = ResourceUp.statDescriptionLong2.ToString();
            }
            else  if (j == 2 && ResourceUp.statRating3 != 0)
            {
                selectedItemStatDescription[j].text = ResourceUp.statDescription3.ToString();
                selectedItemStatRating[j].text = ResourceUp.statRating3.ToString();
                selectedItemStatDescriptionLong[j].text = ResourceUp.statDescriptionLong3.ToString();
            }
                
                
        }

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

    public void OnResourceTabClicked(bool show)
    {
        canvasGroup.interactable = show;
        canvasGroup.blocksRaycasts = show;
        canvasGroup.alpha = show ? 1 : 0;
        QueryEntities();
        Count();
        if(defaultBtn == null || !show) return;
        defaultBtn.Select();

    }

    void QueryEntities()
    {
        var itemQuery = _manager.CreateEntityQuery(ComponentType.ReadOnly<ResourceItemComponent>());
        var itemGroup = itemQuery.ToComponentDataArray<ResourceItemComponent>(Allocator.Persistent);
        var resourceItems = new List<ResourceItemComponent>();

        Debug.Log("RESOURCE " + itemGroup.Length);
        for (var i = 0; i < itemGroup.Length; i++)
        {
            if (itemGroup[i].itemPickedUp)
            {
                resourceItems.Add(itemGroup[i]);
            }
        }
        itemGroup.Dispose();

        passedResourceItemComponents = resourceItems;
    }

    public void ShowMenu()
    {

        QueryEntities();
        Count();
        ShowLabels();

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


[UpdateInGroup(typeof(SimulationSystemGroup))]


public partial class ResourceSystem : SystemBase
{



    protected override void OnUpdate()
    {


        //var ecb = new EntityCommandBuffer(Allocator.Persistent);
        //ecb.Playback(EntityManager);
        //ecb.Dispose();

    }



}