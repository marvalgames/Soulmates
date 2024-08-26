using Rewired;
using Sandbox.Player;
using Unity.Cinemachine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public struct CameraControlsComponent : IComponentData
{
    public LocalTransform localTransform;
    public float fov;
    public bool active;
    public float3 forward;
    public float3 right;
}

public class CameraControls : MonoBehaviour
{
    public Rewired.Player player;
    public int playerId; // The Rewired player id of this character
    private bool changeX, changeY;
    [Header("Free Look Rotation")] public CinemachineCamera freeLook;

    public CinemachineOrbitalFollow follow;
    public CinemachineRotationComposer RotationComposer;
    public CinemachineRecomposer Recomposer;
    
    public float minValueX = -180;
    public float maxValueX = 180;
    public float minHeight;
    [Tooltip("Max height is relative to FOV")]
    public float maxHeight = 45f;
    public float minScale = .25f;
    public float maxScale = 5;
    public float xAxisValue;
    public float heightY;
    public float multiplierX = 30;
    public float multiplierY = 24;
    public float multiplierScale = 4;

    private float startHeight;
    private float startScale;
    private Vector3 startRotationDamping;
    private float scaleValue;
    [SerializeField] bool aimMode;
    //private float fovHeightAdj;


    [SerializeField] PlayerWeaponAim playerWeaponAimReference;
 
    void Start()
    {
        if (!ReInput.isReady) return;
        player = ReInput.players.GetPlayer(playerId);
        startHeight = follow.VerticalAxis.Value;
        xAxisValue = follow.HorizontalAxis.Value;
        startRotationDamping = RotationComposer.Damping;
        startScale = Recomposer.ZoomScale;
        scaleValue = startScale;
        heightY = startHeight;
        ChangeFov(false);
        

    }

    void LateUpdate()
    {
        var controller = player.controllers.GetLastActiveController();
        var aimDisabled = false;
        if (playerWeaponAimReference)
        {
            aimMode = playerWeaponAimReference.aimMode;
            aimDisabled = playerWeaponAimReference.aimDisabled;
        }
        
        if (aimMode)
        {
            RotationComposer.Damping = startRotationDamping * 10;
        }
        else
        {
            RotationComposer.Damping = startRotationDamping * 1;
        }

        

        if (controller == null || aimMode && !aimDisabled) return;//if aim Disabled completely then always allow right stick cam controls

        var gamePad = controller.type == ControllerType.Joystick;
        var keyboard = controller.type == ControllerType.Keyboard;
        bool modifier = player.GetButton("RightTrigger"); // get the "held" state of the button

        changeX = true;
        changeY = true;

        if (player.GetAxis("RightVertical") <= -.25)
        {
            if (!modifier)
            {
                heightY += Time.deltaTime * multiplierY;
            }
            else
            {
                scaleValue += Time.deltaTime * multiplierScale;
            }
            ChangeFov(modifier);
        }
        else if (player.GetAxis("RightVertical") >= .25)
        {
            if (!modifier)
            {
                heightY -= Time.deltaTime * multiplierY;
            }
            else
            {
                scaleValue -= Time.deltaTime * multiplierScale;
            }

            ChangeFov(modifier);
        }

        if (player.GetAxis("RightHorizontal") <= -.25)
        {
            xAxisValue -= Time.deltaTime * multiplierX;
            ChangeFov(modifier);
        }
        else if (player.GetAxis("RightHorizontal") >= .25)
        {
            xAxisValue += Time.deltaTime * multiplierX;
            ChangeFov(modifier);
        }
    }


    public void ChangeFov(bool modifier)
    {
        if (freeLook)
        {
            
            
            if (changeX && !modifier)
            {
                //xAxisValue = math.clamp(xAxisValue, minValueX, maxValueX);
                follow.HorizontalAxis.Value = xAxisValue;
            }

            if (changeY && !modifier)
            {
                //var adjMaxHeight = startFov / fovValue * maxHeight;
                heightY = math.clamp(heightY, minHeight, maxHeight);
                follow.VerticalAxis.Value = heightY;
            }
            else if (changeY)
            {
                scaleValue = math.clamp(scaleValue, minScale, maxScale);
                Recomposer.ZoomScale = scaleValue;
            }
            
        }
    }
}