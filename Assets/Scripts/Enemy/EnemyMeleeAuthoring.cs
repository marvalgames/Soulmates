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

        [Header("EFFECTS")] public AudioSource moveAudioSource;
        public AudioClip moveAudioClip;
        public ParticleSystem moveParticleSystem;
    }
    public class MovesClass
    {
        public AudioSource moveAudioSource;
        //public AudioClip moveAudioClip;
        //public ParticleSystem moveParticleSystem;
    }

    public class MovesClassHolder : IComponentData
    {
        public List<MovesClass> movesClassList = new();
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

        class EnemyMeleeBaker : Baker<EnemyMeleeAuthoring>
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
                foreach (var movesComponentElement in authoring.moveList.Select(move => new MovesComponentElement
                         {
                             active = authoring.active,
                             triggerType = move.triggerType,
                             animationType = move.animationType,
                             target = move.target,
                             weight = move.weight
                         }))
                {
                    buffer.Add(movesComponentElement);
                }

                // var movesClassList = (from t in authoring.moveList
                //     let moveClass = new MovesClass
                //     {
                //         //moveAudioClip = t.moveAudioClip, 
                //         moveAudioSource = t.moveAudioSource
                //         //moveParticleSystem = t.moveParticleSystem
                //     }
                //     select t).ToList();

                var movesClassList = new List<MovesClass>();
                for (int i = 0; i < authoring.moveList.Count; i++)
                {
                    var moveClass = new MovesClass
                    {
                        moveAudioSource = authoring.moveList[i].moveAudioSource
                    };
                    movesClassList.Add(moveClass);
                }
                var movesClassHolder = new MovesClassHolder
                {
                    movesClassList = movesClassList
                };


                AddComponentObject(e, movesClassHolder);
            }
        }
    }
}