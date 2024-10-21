using Rukhanka;
using Sandbox.Player;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Animate
{
    public partial struct AnimatorEntitySystem : ISystem
    {
        private bool hasRun;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if(hasRun) return;
            hasRun = true;
            var playerCombatStateName = new FixedString512Bytes("Locomotion Combat");
            var playerCombatStateHash = FixedStringExtensions.CalculateHash32(playerCombatStateName);
            var enemyCombatStateName = new FixedString512Bytes("jab");
            var enemyCombatStateHash = FixedStringExtensions.CalculateHash32(enemyCombatStateName);
            var job = new AnimatorEntityJob()
            {
                playerCombatStateHash = playerCombatStateHash,
                enemyCombatStateHash = enemyCombatStateHash,
                playerGroup = SystemAPI.GetComponentLookup<PlayerComponent>(),
                enemyGroup = SystemAPI.GetComponentLookup<EnemyComponent>()
            };
            job.ScheduleParallel();
        }


        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }
    }


    [BurstCompile]
    partial struct AnimatorEntityJob : IJobEntity
    {
        public uint playerCombatStateHash;
        public uint enemyCombatStateHash;
        [ReadOnly] public ComponentLookup<PlayerComponent> playerGroup;
        [ReadOnly] public ComponentLookup<EnemyComponent> enemyGroup;

        void Execute(ref DynamicBuffer<AnimationToProcessComponent> animationToProcessComponents,
            in DynamicBuffer<AnimatorControllerParameterComponent> animatorParameters,
            in DynamicBuffer<AnimatorControllerLayerComponent> animatorLayers,
            ref AnimatorEntityComponent animatorEntity, in Entity entity)
        {
            ScriptedAnimator.ResetAnimationState(ref animationToProcessComponents);
            var controllerBlob = animatorLayers[0].controller;
            var controllerAnimationsBlob = animatorLayers[0].animations;
            ref var layerBlob = ref controllerBlob.Value.layers[0];
            //Debug.Log("Length " + layerBlob.states[0].speed);

            if (playerGroup.HasComponent(entity))
            {
                var playerCombatIndex =
                    ScriptedAnimator.GetStateIndexInControllerLayer(controllerBlob, 0, (uint)playerCombatStateHash);
                Debug.Log("PLAYER COMBAT STATE HASH  " + playerCombatIndex);
                animatorEntity.playerCombatStateID = playerCombatIndex;
            }

            if (enemyGroup.HasComponent(entity))
            {
                var enemyCombatIndex =
                    ScriptedAnimator.GetStateIndexInControllerLayer(controllerBlob, 0, (uint)enemyCombatStateHash);
                Debug.Log("ENEMY COMBAT STATE HASH  " + enemyCombatIndex);
                animatorEntity.enemyCombatStateID = enemyCombatIndex;
            }
        }
    }
}