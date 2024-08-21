using System;
using Sandbox.Player;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.VFX;


[System.Serializable]
public class PlayerMoveGameObjectClass : IComponentData
{
    [FormerlySerializedAs("audioSource")] public GameObject audioSourceGo; 
    [FormerlySerializedAs("vfxSystem")] public GameObject vfxSystemGo;
    public AudioClip clip;
}

public class PlayerMovementAuthoring : MonoBehaviour
{
    public float rotateSpeed = 15;
    public float combatRotateSpeed = 15;
    public bool snapRotation = false;
    public float dampTime = 0;
    public bool move2d = false;
    public float3 startPosition;
    public bool inputDisabled = false;
    public float stepRate = 2;
    public float fallingFramesMax = 18;
    public float inAirNegativeForce = -6;
    public float onGroundNegativeForce = -9.81f;
    public float approachStairBoost = 7;
    public float checkGroundDistance = .75f;
    public float checkNearGroundMultiplier = .25f;

    [Tooltip("Y Position of character to start Ray Down")]
    public float checkGroundStartY = 0;

    public float checkRadius = .1f;
    
    public GameObject AudioSource;
    public AudioClip AudioClip;
    public GameObject vfxPrefab;
    
}


public class PlayerMovementBaker : Baker<PlayerMovementAuthoring>
{
    public override void Bake(PlayerMovementAuthoring authoring)
    {
        var position = authoring.transform.position;
        var e = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);
        AddComponent(e, new PlayerMoveComponent()
        {
            combatRotateSpeed = authoring.combatRotateSpeed,
            rotateSpeed = authoring.rotateSpeed,
            snapRotation = authoring.snapRotation,
            dampTime = authoring.dampTime,
            move2d = authoring.move2d,
            startPosition = position,
            inputDisabled = authoring.inputDisabled,
            stepRate = authoring.stepRate
        });

        
        // Register the Prefab in the Baker
        // var entityPrefab = GetEntity(authoring.vfxPrefab, TransformUsageFlags.Dynamic);
        // Add the Entity reference to a component for instantiation later
        // var entity = GetEntity(TransformUsageFlags.Dynamic);
        //AddComponent(entity, new EntityPrefabComponent() {moveVfxSystem = entityPrefab});
        
        AddComponent(e, new ApplyImpulseComponent
        {
            Force = 0,
            Direction = Vector3.zero,
            Grounded = false,
            checkRadius = authoring.checkRadius,
            fallingFramesMaximum = authoring.fallingFramesMax,
            inAirNegativeForce = authoring.inAirNegativeForce,
            ApproachStairBoost = authoring.approachStairBoost,
            checkGroundDistance = authoring.checkGroundDistance,
            checkNearGroundMultiplier = authoring.checkNearGroundMultiplier,
            checkGroundStartY = authoring.checkGroundStartY,
            OnGroundNegativeForce = authoring.onGroundNegativeForce
        });
        //GameObject vfxSystemGo = null;
        //if (authoring.vfxPrefab != null) vfxSystemGo = authoring.vfxPrefab;
        //pass  this to playermove mb and set VFX effect there - for some reason if set in Sub-Scene it ignores parameters
        AddComponentObject(GetEntity(authoring, TransformUsageFlags.Dynamic),
            new PlayerMoveGameObjectClass()
            {
                clip = authoring.AudioClip,
                vfxSystemGo = authoring.vfxPrefab,
                audioSourceGo = authoring.AudioSource
            }
        );
    }
}