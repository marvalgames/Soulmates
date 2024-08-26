using System;
using Collisions;
using Unity.Entities;
using UnityEngine;

public enum AnimationType
{
    None,
    Punch,
    Kick,
    Swing,
    Aim,
    Locomotion,
    Lowering,
    BossStrike,
    JumpStart,
    DashRoll,
    Deflect,
    Melee
}

[Serializable]
public class Moves
{
    public bool active = true;
    public TriggerType triggerType;

    [Header("TARGETING")]
    public AnimationType animationType;
    public Vector3 target;
    public float weight;
    public Entity targetEntity;//not shown
    public float timeAfterMove = .5f;

    [Header("EFFECTS")]
    public AudioSource moveAudioSource;
    public AudioClip moveAudioClip;
    public ParticleSystem moveParticleSystem;

            
    private float calculatedStrikeDistanceZoneBegin;


    // public void CalculateStrikeDistanceFromPinPosition(Transform _transform)
    // {
    //     if (pin == null) return;
    //     
    //     var offset = .23f;
    //     calculatedStrikeDistanceZoneBegin = Vector3.Distance(_transform.position, pin.position) - offset;//.25 
    //     //Debug.Log("strike start " + calculatedStrikeDistanceZoneBegin);
    //
    // }

}