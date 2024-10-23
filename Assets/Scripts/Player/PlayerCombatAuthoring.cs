using System.Collections.Generic;
using Enemy;
using Unity.Entities;
using UnityEngine;
using UnityEngine.VFX;

namespace Sandbox.Player
{
    
    public class PlayerCombatAuthoring : MonoBehaviour
    {
        [SerializeField] private bool active = true;
        [SerializeField] private float hitPower = 100;

        public List<Moves> moveList;
        public GameObject audioSourceMeleePrefab;
        public VisualEffect vfxGraph;
        class PlayerCombatBaker : Baker<PlayerCombatAuthoring>
        {
            public override void Bake(PlayerCombatAuthoring authoring)
            {
                var e = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);
                
                //AddComponent(vfxEntity, new VfxComponentTag());
                
                AddComponent(e, new MeleeComponent
                {
                    Available = authoring.active,
                    hitPower = authoring.hitPower,
                    gameHitPower = authoring.hitPower,
                    instantiated = false
                });

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
                    
                    var vfxEntity = GetEntity(authoring.moveList[i].moveParticleSystemPrefab, TransformUsageFlags.Dynamic);
                    Debug.Log("vfxEntity " + vfxEntity);

                    
                    var addMove = new MovesClass
                    {
                        meleeAudioSource = authoring.audioSourceMeleePrefab, moveAudioClip = move.moveAudioClip,
                        moveParticleSystem = move.moveParticleSystemPrefab,
                        moveVfxPrefabEntity = vfxEntity
                    };
                    movesClassList.Add(addMove);

                    AddComponent(vfxEntity, new VfxGraphSubSceneComponent());

                }

                var movesClassHolder = new MovesClassHolder
                {
                    //inconsistent adding AudioSource to holder AND to each element - That is only needed for the Clip and VFX
                    vfxGraph = authoring.vfxGraph,
                    movesClassList = movesClassList,
                    meleeAudioSourcePrefab = authoring.audioSourceMeleePrefab,
                    moveCount = authoring.moveList.Count
                };
                
                AddComponentObject(e, movesClassHolder);
                //AddComponentObject(e, authoring.visualEffectPrefab);
            }
        }
    }
}