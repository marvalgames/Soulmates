using System.Collections.Generic;
using Enemy;
using Unity.Entities;
using UnityEngine;

namespace Sandbox.Player
{
    public class PlayerCombatAuthoring : MonoBehaviour
    {
        [SerializeField] private bool active = true;
        [SerializeField] private float hitPower = 100;

        public List<Moves> moveList;
        public GameObject audioSourceMeleePrefab;

        class PlayerCombatBaker : Baker<PlayerCombatAuthoring>
        {
            public override void Bake(PlayerCombatAuthoring authoring)
            {
                var e = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);
                AddComponent(e, new MeleeComponent
                {
                    Available = authoring.active,
                    hitPower = authoring.hitPower,
                    gameHitPower = authoring.hitPower
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
                    var addMove = new MovesClass
                        { meleeAudioSource = authoring.audioSourceMeleePrefab, moveAudioClip = move.moveAudioClip };
                    movesClassList.Add(addMove);
                }

                var movesClassHolder = new MovesClassHolder
                {
                    //inconsistent adding AudioSource to holder AND to each element - That is only needed for the Clip and VFX 
                    movesClassList = movesClassList,
                    meleeAudioSourcePrefab = authoring.audioSourceMeleePrefab
                };


                AddComponentObject(e, movesClassHolder);
            }
        }
    }
}