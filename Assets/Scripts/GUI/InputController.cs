using UnityEngine;
using Rewired;
using Unity.Entities;
using Unity.Mathematics;


public struct InputControllerComponent : IComponentData
{
    public Entity playerEntity;
    public int playerId; // The Rewired player id of this character
    public float leftStickX;
    public float leftStickY;
    public bool leftStickXreleased;
    public bool leftStickYreleased;
    public float dpadX;
    public float dpadY;
    public bool dpadXreleased;
    public bool dpadYreleased;
    public float deadZone;
    public bool rotating;
    public bool mouse;

    public bool dpadRight;
    public bool dpadLeft;
    public bool dpadUp;
    public bool dpadDown;


    public float leftTriggerLast;
    public float rightTriggerLast;
    public bool leftTriggerPressed;
    public bool rightTriggerPressed;
    public float leftTriggerChange;
    public float rightTriggerChange;

    public bool leftTriggerDown;
    public bool rightTriggerDown;


    public bool rightStickPressed;

    public bool leftBumperPressed;
    public bool leftBumperReleased;
    public float leftBumperValue;

    public bool rightBumperPressed;
    public bool rightBumperReleased;
    public float rightBumperValue;

    public float leftTriggerValue;
    public float rightTriggerValue;


    public bool buttonA_Pressed;
    public bool buttonA_held;
    public bool buttonA_Tap;
    public bool buttonA_Press;
    public bool buttonA_SinglePress;
    public bool buttonA_DoublePress;
    public bool buttonA_Released;

    public bool buttonB_Pressed;
    public bool buttonB_held;
    public bool buttonB_Tap;
    public bool buttonB_Press;
    public bool buttonB_SinglePress;
    public bool buttonB_DoublePress;
    public bool buttonB_Released;

    public bool buttonX_Pressed;
    public bool buttonX_held;
    public bool buttonX_Tap;
    public bool buttonX_Press;
    public double buttonTimeX_UnPressed;
    public bool buttonX_SinglePress;
    public bool buttonX_DoublePress;
    public bool buttonX_Released;

    public bool buttonY_Pressed;
    public bool buttonY_held;
    public bool buttonY_Tap;
    public bool buttonY_Press;
    public bool buttonY_SinglePress;
    public bool buttonY_DoublePress;
    public bool buttonY_Released;

    public bool buttonSelect_Pressed;
    public bool buttonSelect_held;
    public bool buttonSelect_Released;

    public double buttonTimePressed;
    public float maxTapTime;
    public double comboBufferTimeStart;
    public double comboBufferTimeEnd;
    public double comboBufferTimeMax;

    public float2 mousePosition;


}


public class InputController : MonoBehaviour
{
    [HideInInspector]
    public bool mouse;
    public bool rotating = false;


    public Rewired.Player Player;
    public int playerId = 0; // The Rewired player id of this character
    
    
    public double buttonTimePressed;
    public bool disableInputController;

    void Start()
    {
        if (!ReInput.isReady) return;
        Player = ReInput.players.GetPlayer(playerId);

    }
  
  

}

