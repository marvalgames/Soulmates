using System;
using System.Collections.Generic;
using System.Linq;
using Collisions;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Enemy
{
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


    //in inspector only
    //broken up into managed and unmanaged
    [Serializable]
    public class Moves : IComponentData
    {
        public bool active = true;
        public TriggerType triggerType;
        [Header("TARGETING")] public AnimationType animationType;
        public Vector3 target;

        public float weight;

        //public Entity targetEntity;//not shown
        public float timeAfterMove = .5f;

        [Header("EFFECTS")] public GameObject meleeAudioSourcePrefab;
        public AudioClip moveAudioClip;
        public GameObject moveParticleSystemPrefab;
    }

    public class MovesClass : IComponentData
    {
        public GameObject meleeAudioSource;
        public AudioClip moveAudioClip;
        public GameObject moveParticleSystem;
        public GameObject moveParticleSystemInstance;
    }

    public class MovesClassHolder : IComponentData
    {
        public List<MovesClass> movesClassList = new();
        public GameObject meleeAudioSourcePrefab;
        public int moveCount;

    }

    public class MovesInstance : IComponentData
    {
        public GameObject meleeAudioSourceInstance;
    }

    [InternalBufferCapacity(8)]
    public struct MovesComponentElement : IBufferElementData
    {
        public bool active;
        public TriggerType triggerType;
        public AnimationType animationType;
        public Vector3 target;
        public float weight;
        public Entity targetEntity;
        public float timeAfterMove;
    }


    public class EnemyMeleeAuthoring : MonoBehaviour
    {
        public bool active = true;
        public float hitPower = 10f;
        public bool allEnemyCollisionsCauseDamage;
        public List<Moves> moveList;
        public GameObject audioSourceMeleePrefab;
        
        private class EnemyMeleeBaker : Baker<EnemyMeleeAuthoring>
        {
            public override void Bake(EnemyMeleeAuthoring authoring)
            {
                var e = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);
                AddComponent(e, new MeleeComponent
                    {
                        Available = authoring.active,
                        hitPower = authoring.hitPower,
                        gameHitPower = authoring.hitPower,
                        anyTouchDamage = authoring.allEnemyCollisionsCauseDamage
                    }
                );

                var buffer = AddBuffer<MovesComponentElement>(e);
                foreach (var move in authoring.moveList)
                {
                    var movesComponentElement = new MovesComponentElement
                    {
                        active = authoring.active,
                        triggerType = move.triggerType,
                        animationType = move.animationType,
                        target = move.target,
                        weight = move.weight
                    };
                    buffer.Add(movesComponentElement);
                }

                var movesClassList = new List<MovesClass>();
                for (var i = 0; i < authoring.moveList.Count; i++)
                {
                    var move = authoring.moveList[i];
                    var addMove = new MovesClass
                        { meleeAudioSource = authoring.audioSourceMeleePrefab, moveAudioClip = move.moveAudioClip };
                    movesClassList.Add(addMove);
                }
                var movesClassHolder = new MovesClassHolder
                {
                    //inconsistent adding AudioSource to holder AND to each element - That is only needed for the Clip and VFX 
                    movesClassList = movesClassList,
                    moveCount = authoring.moveList.Count,
                    meleeAudioSourcePrefab = authoring.audioSourceMeleePrefab
                };


                AddComponentObject(e, movesClassHolder);
            }
        }
    }
}