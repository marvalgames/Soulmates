using System;
using Unity.Entities;
using UnityEngine;
using Rewired;
using Sandbox.Player;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public struct Pause : IComponentData
{

}

public enum GameControllerMap
{
    DefaultMap, 
    MenuMap,
    DialogMap
    
}

public class GameInterface : MonoBehaviour
{
    public static event Action HideMenuEvent;
    public static event Action SelectClickedEvent;
    public static GameInterface instance = null;

    private int sceneIndex = 0;

    [Header("READ ONLY")]
    public bool Paused = false;
    public bool StateChange = false;
    public Rewired.Player player;
    [HideInInspector]
    public int playerId = 0; // The Rewired player id of this character
    public bool startPaused = true;

    [HideInInspector]
    public bool PauseMenuDisabled;
    [HideInInspector]
    public GameControllerMap ActiveGameControllerMap;

    void Awake()
    {

        //Check if there is already an instance of SoundManager
        if (instance == null)
            //if not, set it to this.
            instance = this;
        //If instance already exists:
        else if (instance != this)
            //Destroy this, this enforces our singleton pattern so there can only be one instance of SoundManager.
            Destroy(gameObject);
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        if (!ReInput.isReady) return;
        player = ReInput.players.GetPlayer(playerId);
        Paused = startPaused;
        StateChange = true;
        sceneIndex = SceneManager.GetActiveScene().buildIndex;
        if (sceneIndex <= 1)
        {
            EnableControllerMaps(false, isMenu: true, false);
        }
        else
        {
            EnableControllerMaps(true, isMenu: false, false);
        }
    }

    private void Update()
    {
        if (player.GetButtonDown("select") || 
            player.GetButtonDown("UICancel")
           )
        {
            StateChange = true;
            Paused = !Paused;
            SelectClicked();

        }


    }
    
    public void EnableControllerMaps(bool isDefault, bool isMenu, bool isDialog)
    {
        //Debug.Log("enable DIALOG MENU ");
        var player = ReInput.players.GetPlayer(0);
        if (sceneIndex <= 1) isMenu = true;//always true in menu or loader scene index


        foreach (ControllerMap map in player.controllers.maps.GetAllMapsInCategory("Default", ControllerType.Joystick))
        {
            if(!isMenu) map.enabled = isDefault; // set the enabled state on the map
        }

        foreach (ControllerMap map in player.controllers.maps.GetAllMapsInCategory("Default", ControllerType.Keyboard))
        {
            if(!isMenu) map.enabled = isDefault; // set the enabled state on the map
        }
        
        foreach (ControllerMap map in player.controllers.maps.GetAllMapsInCategory("Menu", ControllerType.Joystick))
        {
            map.enabled = isMenu; // set the enabled state on the map
        }

        foreach (ControllerMap map in player.controllers.maps.GetAllMapsInCategory("Menu", ControllerType.Keyboard))
        {
            map.enabled = isMenu; // set the enabled state on the map
        }
        
        foreach (ControllerMap map in player.controllers.maps.GetAllMapsInCategory("Dialog", ControllerType.Joystick))
        {
            if(!isMenu) map.enabled = isDialog; // set the enabled state on the map
        }

        foreach (ControllerMap map in player.controllers.maps.GetAllMapsInCategory("Dialog", ControllerType.Keyboard))
        {
            if(!isMenu) map.enabled = isDialog; // set the enabled state on the map
        }

        if (isDefault)
        {
            instance.ActiveGameControllerMap = GameControllerMap.DefaultMap;
        }
        else if (isMenu)
        {
            instance.ActiveGameControllerMap = GameControllerMap.MenuMap;
        }
        else if (isDialog)
        {
            instance.ActiveGameControllerMap = GameControllerMap.DialogMap;
        }
        
     
    }
  

    public void SelectClicked()//only called with button from system no menu item currently
    {

        SelectClickedEvent?.Invoke();//pause menu subscribes to this event to show pause menu
    }

}



[UpdateInGroup(typeof(InitializationSystemGroup))]
//[RequireMatchingQueriesForUpdate]

public partial class GameInterfaceSystem : SystemBase
{
    private PhysicsSimulationGroup _physicsSystemGroup;
    private PlayerMoveSystem _playerMoveSystem;
    private InputControllerSystemUpdate _inputControllerSystem;
    protected override void OnCreate()
    {
        _physicsSystemGroup = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<PhysicsSimulationGroup>();
        _inputControllerSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<InputControllerSystemUpdate>();
    }


    protected override void OnUpdate()
    {
        if(GameInterface.instance == null) return; 
        
        var paused = GameInterface.instance.Paused;
        var stateChange = GameInterface.instance.StateChange;
        //ECS 1.0
        _physicsSystemGroup.Enabled = !paused;
        _inputControllerSystem.Enabled = !paused;
        //_playerMoveSystem.Enabled = !paused;

        if (stateChange)
        {

            //Debug.Log("STATE CHANGE " + paused);
            if (paused)
            {
                GameInterface.instance.EnableControllerMaps(false, true, false);
               
            }
            else
            {
                GameInterface.instance.EnableControllerMaps(true, false, false);
               
            }
            GameInterface.instance.StateChange = false;
            Entities.WithoutBurst().WithAll<EntityFollow>().WithStructuralChanges().ForEach((Entity entity) =>
            {

                if (paused)
                {
                    EntityManager.AddComponent<Pause>(entity);
                }
                else
                {
                    EntityManager.RemoveComponent<Pause>(entity);
                }
            }
            ).Run();


            Entities.WithoutBurst().ForEach((Entity entity, Animator animator) =>
            {
                animator.speed = paused ? 0 : 1;
            }
            ).Run();


            Entities.WithoutBurst().ForEach((Entity entity, NavMeshAgent agent) =>
            {
                if(agent.isOnNavMesh)
                {
                    agent.isStopped = paused;
                }
            }
            ).Run();


        }



    }
}


