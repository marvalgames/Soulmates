using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;


[Serializable]
public struct EvadeComponent : IComponentData
{
    public bool evadeStrike;
    public float evadeStrikeRating;
    public bool InEvade;
    public float evadeMoveTime;
    public bool randomEvadeMoveTime;
    public float evadeMoveSpeed;
    public float originalEvadeMoveSpeed;
    public bool zMovement;
    public float EvadeMoveTimer;
    public float addX;
    public float addZ;
    public float3 agentStart;
    public bool startAnimation;

}



public class EvadeComponentAuthoring : MonoBehaviour
{
    public float evadeMoveTime = 2.0f;
    public bool zMovement = true;
    public float evadeMoveSpeed = 5;
    public float evadeStrikeRating = 8;
    public bool randomEvadeMoveTime = true;

    class EvadeComponentBaker : Baker<EvadeComponentAuthoring>
    {
        public override void Bake(EvadeComponentAuthoring authoring)
        {
            var e = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);
            AddComponent(e, 
                new EvadeComponent
                {
                    evadeMoveTime = authoring.evadeMoveTime,
                    evadeStrikeRating = authoring.evadeStrikeRating,
                    originalEvadeMoveSpeed = authoring.evadeMoveTime,
                    evadeMoveSpeed = authoring.evadeMoveSpeed,
                    zMovement = authoring.zMovement,
                    addX = 1,
                    addZ = 0,
                    randomEvadeMoveTime = authoring.randomEvadeMoveTime

                }); ;
        }
    }

   
}
