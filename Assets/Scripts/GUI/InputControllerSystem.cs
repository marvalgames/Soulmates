using Rewired;
using Sandbox.Player;
using Unity.Entities;


[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[RequireMatchingQueriesForUpdate]
public partial class InputControllerSystemUpdate : SystemBase
{
    public Rewired.Player player;
    public int playerId = 0; // The Rewired player id of this character

    protected override void OnCreate()
    {
    }

    protected override void OnStartRunning()
    {
        //base.OnStartRunning();
        if (!ReInput.isReady) return;
        player = ReInput.players.GetPlayer(playerId);

    }

    protected override void OnUpdate()
    {
        
        
        Entities.WithoutBurst().WithAll<PlayerComponent>().ForEach((ref InputControllerComponent inputController) =>
        {

            inputController.leftStickX = player.GetAxis("Move Horizontal");
            inputController.leftStickY = player.GetAxis("Move Vertical");

            inputController.rightStickPressed = player.GetButtonDown("RightStickAction");
            
            inputController.leftBumperPressed = player.GetButtonDown("LeftBumper");
            inputController.leftBumperReleased = player.GetButtonUp("LeftBumper");

            
            inputController.rightBumperPressed = player.GetButtonDown("RightBumper");
            
            
            inputController.leftTriggerPressed = player.GetButtonDown("LeftTrigger");
            inputController.rightTriggerPressed = player.GetButtonUp("RightTrigger");

            
            //
            inputController.buttonTimePressed = player.GetButtonTimePressed("FireA");
            inputController.buttonA_Pressed = player.GetButtonDown("FireA");
            inputController.buttonA_held = player.GetButton("FireA");
            inputController.buttonA_Released = player.GetButtonUp("FireA");
            inputController.buttonA_SinglePress = player.GetButtonSinglePressDown("FireA");
            inputController.buttonA_DoublePress = player.GetButtonDoublePressDown("FireA");
            
            //B
            inputController.buttonB_Pressed = player.GetButtonDown("FireB");
            inputController.buttonB_held = player.GetButton("FireB");
            inputController.buttonB_Released = player.GetButtonUp("FireB");
            inputController.buttonB_SinglePress = player.GetButtonSinglePressDown("FireB");
            inputController.buttonB_DoublePress = player.GetButtonDoublePressDown("FireB");
            
            //X
            inputController.buttonX_Tap = false;
            inputController.buttonX_Press = false;
            if (player.GetButtonTimedPressUp("FireX", 0f, inputController.maxTapTime))
            { 
                inputController.buttonX_Tap = true;
            }
            else if (player.GetButtonTimedPressDown("FireX", inputController.maxTapTime))
            {
                inputController.buttonX_Press = true;
            }
            inputController.buttonX_Pressed = player.GetButtonDown("FireX");
            inputController.buttonX_held = player.GetButton("FireX");
            inputController.buttonX_Released = player.GetButtonUp("FireX");
            inputController.buttonTimeX_UnPressed = player.GetButtonTimeUnpressed("FireX");
            //Y
            inputController.buttonY_Tap = false;
            inputController.buttonY_Press = false;
            if (player.GetButtonTimedPressUp("FireY", 0f, inputController.maxTapTime))
            { 
                inputController.buttonY_Tap = true;
            }
            else if (player.GetButtonTimedPressDown("FireY", inputController.maxTapTime))
            {
                inputController.buttonY_Press = true;
            }
            inputController.buttonY_Pressed = player.GetButtonDown("FireY");
            inputController.buttonY_held = player.GetButton("FireY");
            inputController.buttonY_Released = player.GetButtonUp("FireY");
            
            inputController.buttonSelect_Pressed = player.GetButtonDown("Select");
            inputController.buttonSelect_held = player.GetButton("Select");
            inputController.buttonSelect_Released = player.GetButtonUp("Select");
            
            inputController.dpadX = player.GetAxis("Dpad Horizontal");
            inputController.dpadY = player.GetAxis("Dpad Vertical");


        }).Run();
    }
}


public partial class TimeStep : SystemBase
{
    protected override void OnCreate()
    {
        var group = World.GetExistingSystemManaged<FixedStepSimulationSystemGroup>();
        group.Timestep = 1/120f;
    }


    protected override void OnUpdate()
    {



    }
}



