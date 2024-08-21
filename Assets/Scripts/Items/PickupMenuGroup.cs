using Rewired;
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


public struct PickupMenuComponent : IComponentData
{
    public bool showMenu;
    public bool exitClicked;
    public bool menuStateChanged;
    public int usedItem;
    public Entity pickupEntity1;
    public bool pickedUp1;
    public bool use1;
    public Entity pickupEntity2;
    public bool pickedUp2;
    public bool use2;
    public bool clearItem1;
    public bool clearItem2;



}

[Serializable]
public class MenuPickupItemData
{
    public int[] ItemIndex = new int[65];

    //public int[] SlotUsed = new int[65];
    public int[] UseSlot = new int[4]; //use buttons 1-4
    public Entity[] ItemEntity = new Entity[65];
    public int CurrentIndex;
    public int Count;
    public int Remain; //how many still available left to choose from pick up list
    public Image Image;
}

[Serializable]
public class PickupItemData
{
    public TextMeshProUGUI longDescriptionlabel; //make class member with all attributes of pickup list 
}

public class PickupMenuGroup : MonoBehaviour
{
    public static event Action<bool> HideSubscriberMenu;
    public bool useUpdated;

    private EntityManager _manager;
    private Entity _entity;
    private readonly List<PowerItemComponent> powerItemComponents = new List<PowerItemComponent>();
    public int playerIndex;

    public List<PowerItemComponent> passedPowerItemComponents = new List<PowerItemComponent>();

    private readonly PowerItemComponent[] useItemComponents = new PowerItemComponent[2];

    AudioSource _audioSource;
    [SerializeField] private List<Button> buttons;
    [SerializeField] private List<Button> useButtons;
    [SerializeField] private Button exitButton;
    public AudioClip clickSound;
    public EventSystem eventSystem;
    private CanvasGroup _canvasGroup;
    [SerializeField] private CanvasGroup headerCanvasGroup;
    [SerializeField] private CanvasGroup resourceMenuCanvasGroup;
    [SerializeField] private CanvasGroup talentMenuCanvasGroup;
    [SerializeField] private Image statsBkImage;
    [SerializeField] private Sprite defaultSprite;
    [SerializeField] private Button defaultBtn;


    [SerializeField] PickupItemData pickupItemData;
    [SerializeField] private TextMeshProUGUI[] pickuplabel;
    [SerializeField] private TextMeshProUGUI[] uselabel;
    [SerializeField] private Button[] useButton = new Button[2]; //blank button with image
    [SerializeField] private Image[] useImage = new Image[2]; //blank button with image


    [SerializeField] private TextMeshProUGUI[] useItemStatDescription;
    [SerializeField] private TextMeshProUGUI[] useItemStatRating;
    [SerializeField] private TextMeshProUGUI[] useItemStatDescriptionLong;



    [SerializeField] private TextMeshProUGUI[] gameViewUse = new TextMeshProUGUI[2];
    [SerializeField] private Button[] gameViewUseButton = new Button[2]; //blank button with image
    [SerializeField] private Image[] gameViewUseImage = new Image[2]; //blank button with image


    [SerializeField] private int buttonClickedIndex;

    public static bool UpdateMenu = true;

    //private bool[] inUsedItemList = new bool[30];
    [NonReorderable]
    public MenuPickupItemData[] menuPickupItem = new MenuPickupItemData[17]; //10 items (buttons) max currently

    //Player player;
    //int _useSlots = 2;
    int _selectedPower;
    bool slot1Used;
    bool slot2Used;
    private bool showMenu;


    void Init()
    {
        passedPowerItemComponents.Clear();
    }

    void Start()
    {
        //string test = "dd";
        if (!ReInput.isReady) return;
        var world = World.DefaultGameObjectInjectionWorld;
        _manager = world.EntityManager;
        _entity = _manager.CreateEntity();
        _manager.AddComponent<PickupMenuComponent>(_entity);
        _manager.AddComponentObject(_entity, this);
        //_manager.CreateEntityQuery()
        //player = ReInput.players.GetPlayer(0);
        _audioSource = GetComponent<AudioSource>();
        _canvasGroup = GetComponent<CanvasGroup>();
        AddMenuButtonHandlers();
        ShowLabels();
        ClearStatRows();
    }

    private void AddMenuButtonHandlers()
    {
        buttons = GetComponentsInChildren<Button>().ToList(); //linq using

        buttons.ForEach((btn) => btn.onClick.AddListener(() =>
            PlayMenuClickSound(clickSound))); //shortcut instead of using inspector to add to each button

        for (var i = 0; i < buttons.Count; i++)
        {
            var temp = i;
            buttons[i].onClick.AddListener(() => { ButtonClickedIndex(temp); });
        }

        buttons[0].Select();

        exitButton.onClick.AddListener(ExitClicked);
    }
    void ButtonClickedIndex(int index)
    {
        buttonClickedIndex = index;
    }

    void PlayMenuClickSound(AudioClip clip)
    {
        _audioSource.PlayOneShot(clip);
        Debug.Log("clip " + clip);
    }




    void QueryEntities()
    {
        var itemQuery = _manager.CreateEntityQuery(ComponentType.ReadOnly<PowerItemComponent>());
        var itemGroup = itemQuery.ToComponentDataArray<PowerItemComponent>(Allocator.Persistent);

        var playerQuery = _manager.CreateEntityQuery(ComponentType.ReadOnly<PlayerComponent>());
        var playerEntities = playerQuery.ToEntityArray(Allocator.TempJob);


        var powerItems = new List<PowerItemComponent>();

        for (var i = 0; i < itemGroup.Length; i++)
        {
            if (itemGroup[i].itemPickedUp && itemGroup[i].pickedUpActor == playerEntities[playerIndex])
            {
                powerItems.Add(itemGroup[i]);
            }
        }

        playerEntities.Dispose();

        passedPowerItemComponents = powerItems;
    }

    void Update()
    {
        if (_entity == Entity.Null || _manager == default) return;
        if(!_manager.HasComponent<PickupMenuComponent>(_entity)) return;
        var pickupMenu = _manager.GetComponentData<PickupMenuComponent>(_entity);
        if (!pickupMenu.clearItem1 && !pickupMenu.clearItem2) return;
        if (pickupMenu.clearItem1)
        {
            useItemComponents[0] = new PowerItemComponent();
            pickupMenu.clearItem1 = false;
        }

        if (pickupMenu.clearItem2)
        {
            useItemComponents[1] = new PowerItemComponent();
            pickupMenu.clearItem2 = false;
        }

        _manager.SetComponentData(_entity, pickupMenu);
        ShowLabels();

    }

    private void ExitClicked()
    {
    }


    public void EnableButtons()
    {
        useButtons[0].interactable = true;
        useButtons[1].interactable = true;
    }
    private void ClearStatRows()
    {
        var rowsToClear = 3; //holder 3 stats max for now
        for (var i = 0; i < rowsToClear; i++)
        {
            useItemStatDescription[i].text = "";
            useItemStatRating[i].text = "";
            useItemStatDescriptionLong[i].text = "";
        }
    }


    public void Count()
    {
        var tempItems = new List<PowerItemComponent>(passedPowerItemComponents);
        powerItemComponents.Clear();
        var powerUps = 16; //change this as we add new powerups

        var first = -1;
        for (var j = 0; j < powerUps; j++)
        {
            var menu = menuPickupItem[j];
            var ico = menu.Image;
            var useSlot1 = menu.UseSlot[0];
            if (useItemComponents[0].pickupEntity == Entity.Null)
            {
                useSlot1 = 0;
            }

            var useSlot2 = menu.UseSlot[1];
            if (useItemComponents[1].pickupEntity == Entity.Null)
            {
                useSlot2 = 0;
            }

            first = -1;
            var count = 0;
            menu.Count = count;
            for (var i = 0; i < tempItems.Count; i++)
            {
                menu.ItemIndex[i] = 0;
                menu.ItemEntity[i] = Entity.Null;
                if ((int)tempItems[i].pickupType == j + 1 && useSlot1 != tempItems[i].pickupEntity.Index &&
                    useSlot2 != tempItems[i].pickupEntity.Index)
                {
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
                powerItemComponents.Add(tempItems[first]);
            }
            else
            {
                powerItemComponents.Add(new PowerItemComponent());
            }


            menu.Image = ico;
            menu.UseSlot[0] = useSlot1;
            menu.UseSlot[1] = useSlot2;
            menuPickupItem[j] = menu;
        }


        ShowLabels();
    }


    public void ShowLabels()
    {
        for (var i = 0; i < uselabel.Length; i++)
        {
            uselabel[i].text = "";
            gameViewUseButton[i].gameObject.SetActive(false);
        }

        for (var i = 0; i < pickuplabel.Length; i++)
        {
            pickuplabel[i].text = "";
        }


        for (var i = 0; i < pickuplabel.Length; i++)
        {
            if (i < powerItemComponents.Count && powerItemComponents[i].count > 0)
            {
                pickuplabel[i].text =
                    powerItemComponents[i].description.ToString() + " " + powerItemComponents[i].count;
                var index = powerItemComponents[i].menuIndex;
                buttons[i + 1].interactable = true;
            }
        
        }


        GameLabels();
    }

    public void GameLabels()
    {
        for (var i = 0; i < useItemComponents.Length; i++)
        {
            var a = 0;
            if (useItemComponents[i].useSlot1 &&
                i == 0) //deletes entity after use so now this is still true :( useslotindex = selected power
            {
                uselabel[0].text = "";
                a = 1;
                useButton[0].interactable = true;
                gameViewUseButton[0].gameObject.SetActive(true);
                uselabel[0].text = useItemComponents[0].description.ToString();
                useImage[0].sprite = menuPickupItem[useItemComponents[0].menuIndex].Image.sprite;
            }

            if (useItemComponents[i].useSlot2 &&
                i == 1) //deletes entity after use so now this is still true :( useslotindex = selected power
            {
                uselabel[1].text = "";
                a = 1;
                useButton[1].interactable = true;
                gameViewUseButton[1].gameObject.SetActive(true);
                uselabel[1].text = useItemComponents[1].description.ToString();
                useImage[1].sprite = menuPickupItem[useItemComponents[1].menuIndex].Image.sprite;
            }

            var color = useImage[i].color;
            color.a = a;
            useImage[i].color = color;
        }

        for (var i = 0; i < gameViewUse.Length; i++)
        {
            gameViewUse[i].text = uselabel[i].text;
            gameViewUseImage[i].sprite = useImage[i].sprite;
        }
    }

 

    public void SelectedPower(int index)
    {
        _selectedPower = index;
        var powerUp = powerItemComponents[_selectedPower];
        pickupItemData.longDescriptionlabel.text = powerUp.longDescription.ToString();
        Debug.Log("sel " + _selectedPower);
        ClearStatRows();
        statsBkImage.sprite = menuPickupItem[index].Image.sprite;

        for (var j = 0; j < 3; j++)
        {
            if (j == 0 && powerUp.statRating1 != 0)
            {
                useItemStatDescription[j].text = powerUp.statDescription1.ToString();
                useItemStatRating[j].text = powerUp.statRating1.ToString();
                useItemStatDescriptionLong[j].text = powerUp.statDescriptionLong1.ToString();
            }
            else if (j == 1 && powerUp.statRating2 != 0)
            {
                useItemStatDescription[j].text = powerUp.statDescription2.ToString();
                useItemStatRating[j].text = powerUp.statRating2.ToString();
                useItemStatDescriptionLong[j].text = powerUp.statDescriptionLong2.ToString();
            }
            else if (j == 2 && powerUp.statRating3 != 0)
            {
                useItemStatDescription[j].text = powerUp.statDescription3.ToString();
                useItemStatRating[j].text = powerUp.statRating3.ToString();
                useItemStatDescriptionLong[j].text = powerUp.statDescriptionLong3.ToString();
            }
        }
    }

   

    public void RemoveUsePower(int useIndex)
    {
        if (useIndex <= 0) throw new ArgumentOutOfRangeException(nameof(useIndex));
        if (slot1Used)
        {
            useIndex = 1;
        }
        else if (slot2Used)
        {
            useIndex = 2;
        }
        else return;
        var index = useItemComponents[useIndex - 1].menuIndex;
        if (useIndex == 1)
        {
            menuPickupItem[index].UseSlot[0] = 0;
            var power = useItemComponents[0];
            var pickupEntity = power.pickupEntity;
            _manager.RemoveComponent<UseItem1>(pickupEntity);
            slot1Used = false;
        }
        else
        {
            menuPickupItem[index].UseSlot[1] = 0;
            var power = useItemComponents[1];
            var pickupEntity = power.pickupEntity;
            _manager.RemoveComponent<UseItem2>(pickupEntity);
            slot2Used = false;
        }

        useItemComponents[useIndex - 1] = new PowerItemComponent();
        Count();
        ShowLabels();
    }

    public void SetSelectedPower(int useIndex)
    {
        if (useIndex <= 0) throw new ArgumentOutOfRangeException(nameof(useIndex));
        
        if (slot1Used)
        {
            useIndex = 2;
        }
        else if (slot2Used)
        {
            useIndex = 1;
        }
        
        var assigned = AssignSelectedPower(useIndex);
        // if (!assigned)
        // {
        //     RemoveUsePower(useIndex);
        // }
    }

    public bool AssignSelectedPower(int useIndex)
    {
        var assigned = false;
        if (powerItemComponents.Count == 0) return false;
        var menuIndex = powerItemComponents[_selectedPower].menuIndex;
        var currentIndex = 0;
        var count = menuPickupItem[menuIndex].Count;
        if (useIndex <= 0 || count <= 0) return false;
        if (_entity == Entity.Null || _manager == default) return false;
        var pickupEntity = menuPickupItem[menuIndex].ItemEntity[currentIndex];
        var usedSlot1 = menuPickupItem[menuIndex].UseSlot[0];
        var usedSlot2 = menuPickupItem[menuIndex].UseSlot[1];
        var pickedUp = powerItemComponents[_selectedPower].itemPickedUp;
        var item = powerItemComponents[_selectedPower];


        if (pickupEntity != Entity.Null && pickedUp == true)
        {
            if (useIndex == 1 && usedSlot1 == 0)
            {
                menuPickupItem[menuIndex].UseSlot[0] = item.pickupEntity.Index;
                item.useSlot1 = true;
                useItemComponents[0] = item;
                useUpdated = true;
                assigned = true;
                slot1Used = true;
                SetUseItemComponent();
            }
            else if (useIndex == 2 && usedSlot2 == 0)
            {
                menuPickupItem[menuIndex].UseSlot[1] = item.pickupEntity.Index;
                item.useSlot2 = true;
                useItemComponents[1] = item;
                useUpdated = true;
                assigned = true;
                slot2Used = true;
                SetUseItemComponent();
            }
        }

        Count();
        ShowLabels();
        return assigned;
    }



    void SetUseItemComponent()
    {
        useUpdated = false;
        var pickupEntity1 = useItemComponents[0].pickupEntity;
        var pickedUp1 = useItemComponents[0].itemPickedUp;
        var use1 = useItemComponents[0].useSlot1;
        var pickupEntity2 = useItemComponents[1].pickupEntity;
        var pickedUp2 = useItemComponents[1].itemPickedUp;
        var use2 = useItemComponents[1].useSlot2;
        
        if (pickupEntity1 != Entity.Null && pickedUp1 == true && use1)
        {
            var power = _manager.GetComponentData<PowerItemComponent>(pickupEntity1);
            power.useSlot1 = true;
            power.useSlot2 = false;
            _manager.SetComponentData(pickupEntity1, power);
            _manager.RemoveComponent<UseItem2>(pickupEntity1);
            _manager.AddComponent<UseItem1>(pickupEntity1);
        }

        if (pickupEntity2 != Entity.Null && pickedUp2 == true && use2)
        {
            var power = _manager.GetComponentData<PowerItemComponent>(pickupEntity2);
            power.useSlot2 = true;
            power.useSlot1 = false;
            _manager.SetComponentData(pickupEntity2, power);
            _manager.RemoveComponent<UseItem1>(pickupEntity2);
            _manager.AddComponent<UseItem2>(pickupEntity2);
            //Debug.Log("add use2 " + pickupEntity2);
        }
        
        
        
    }


    public void ShowMenu()
    {
        HideSubscriberMenu?.Invoke(false);
        GameInterface.instance.EnableControllerMaps(false, true, false);
        GameInterface.instance.Paused = true;
        GameInterface.instance.StateChange = true;
        _canvasGroup.alpha = 1;
        _canvasGroup.interactable = true;
        _canvasGroup.blocksRaycasts = true;
        if (headerCanvasGroup)
        {
            headerCanvasGroup.alpha = 1;
            headerCanvasGroup.interactable = true;
            headerCanvasGroup.blocksRaycasts = true;
        }

        showMenu = true;
        QueryEntities();

        Count();
        ShowLabels();
        defaultBtn.Select();
    }

    public void HideMenu()
    {
        //PauseGame?.Invoke(false);
        GameInterface.instance.EnableControllerMaps(true, false, false);
        GameInterface.instance.Paused = false;
        GameInterface.instance.StateChange = true;
        _canvasGroup.interactable = false;
        _canvasGroup.alpha = 0.0f;
        _canvasGroup.blocksRaycasts = false;
        if (headerCanvasGroup)
        {
            headerCanvasGroup.alpha = 0;
            headerCanvasGroup.interactable = false;
            headerCanvasGroup.blocksRaycasts = false;
        }
        if (resourceMenuCanvasGroup)
        {
            resourceMenuCanvasGroup.alpha = 0;
            resourceMenuCanvasGroup.interactable = false;
            resourceMenuCanvasGroup.blocksRaycasts = false;
        }
        if (talentMenuCanvasGroup)
        {
            talentMenuCanvasGroup.alpha = 0;
            talentMenuCanvasGroup.interactable = false;
            talentMenuCanvasGroup.blocksRaycasts = false;
        }

        showMenu = false;
    }

    public void OnPickupTabClicked(bool show)
    {
        // if (headerCanvasGroup)
        // {
        //     headerCanvasGroup.alpha = show ? 1 : 0;
        //     headerCanvasGroup.interactable = show;
        //     headerCanvasGroup.blocksRaycasts = show;
        // }
        _canvasGroup.interactable = show;
        _canvasGroup.blocksRaycasts = show;
        _canvasGroup.alpha = show ? 1 : 0;
        Count();
        if(defaultBtn == null || !show) return;
        defaultBtn.Select();

        
    }


}


[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial class PickupSystem : SystemBase
{
    protected override void OnUpdate()
    {
     
        if (SystemAPI.HasSingleton<PickupMenuComponent>() == false || LevelManager.instance.endGame) return;
        var ecb = new EntityCommandBuffer(Allocator.Persistent);
        var pickupMenu = SystemAPI.GetSingleton<PickupMenuComponent>();
        pickupMenu.menuStateChanged = false;
        var lStickClicked = ReInput.players.GetPlayer(0).GetButtonDown("LeftStickAction");
        if (pickupMenu.exitClicked && pickupMenu.showMenu)
        {
            pickupMenu.menuStateChanged = true;
            pickupMenu.exitClicked = false;
            pickupMenu.showMenu = false;
        }
        else if (lStickClicked)
        {
            pickupMenu.menuStateChanged = true;
            pickupMenu.showMenu = !pickupMenu.showMenu;
        }

        Entities.WithoutBurst().ForEach((PickupMenuGroup pickupMenuGroup) =>
            {
                if (pickupMenu.usedItem > 0)
                {
                    pickupMenu.usedItem = 0;
                    pickupMenu.menuStateChanged = true;
                    pickupMenuGroup.Count();
                    pickupMenuGroup.ShowLabels();
                }

                if (pickupMenu.menuStateChanged == false) return;
                if (pickupMenu.showMenu)
                {
                    pickupMenuGroup.ShowMenu();
                    pickupMenuGroup.EnableButtons();
                    pickupMenuGroup.ShowLabels();
                }
                else
                {
                    pickupMenuGroup.HideMenu();
                }
            }
        ).Run();


        SystemAPI.SetSingleton(pickupMenu);

        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}


[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial class InputUseItemSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var ecb = new EntityCommandBuffer(Allocator.Persistent);
        var hasPickupMenu = SystemAPI.HasSingleton<PickupMenuComponent>();
        if(!hasPickupMenu) return;

        var hasUse1 = SystemAPI.HasSingleton<UseItem1>();
        var hasUse2 = SystemAPI.HasSingleton<UseItem2>();
        var pickupMenu = SystemAPI.GetSingleton<PickupMenuComponent>();
        if (hasUse1)
        {
            var use1Entity = SystemAPI.GetSingletonEntity<UseItem1>();
            var use1Pressed = ReInput.players.GetPlayer(0).GetButtonDown("Use1");
            if (use1Pressed && SystemAPI.HasComponent<PowerItemComponent>(use1Entity))
            {
                ecb.AddComponent<ImmediateUseComponent>(use1Entity);
                pickupMenu.clearItem1 = true;
            }
        }

        if (hasUse2)
        {
            var use2Entity = SystemAPI.GetSingletonEntity<UseItem2>();
            var use2Pressed = ReInput.players.GetPlayer(0).GetButtonDown("Use2");
            if (use2Pressed && SystemAPI.HasComponent<PowerItemComponent>(use2Entity))
            {
                ecb.AddComponent<ImmediateUseComponent>(use2Entity);
                pickupMenu.clearItem2 = true;

            }
        }
        
        if(hasUse1 || hasUse2) SystemAPI.SetSingleton(pickupMenu);

        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}