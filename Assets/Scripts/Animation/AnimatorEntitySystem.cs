using Rukhanka;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
.using FixedStringName = Unity.Collections.FixedString512Bytes;

namespace Animate
{
    public partial struct AnimatorEntitySystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var stateNameFull = new FixedStringName("combo_one");
            var stateHash = stateNameFull.CalculateHash32();
            var job = new AnimatorEntityJob()
            {
                stateHash = stateHash

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
        public int stateHash;

        void Execute(ref DynamicBuffer<AnimationToProcessComponent> animationToProcessComponents,
            in DynamicBuffer<AnimatorControllerParameterComponent> animatorParameters,
            in DynamicBuffer<AnimatorControllerLayerComponent> animatorLayers, ref AnimatorEntityComponent animatorEntity)
        {
            ScriptedAnimator.ResetAnimationState(ref animationToProcessComponents);
            var controllerBlob = animatorLayers[0].controller;
            var controllerAnimationsBlob = animatorLayers[0].animations;
            ref var layerBlob = ref controllerBlob.Value.layers[0];
            Debug.Log("Length " + layerBlob.states.Length);
            int index = ScriptedAnimator.GetStateIndexInControllerLayer(controllerBlob, 0, (uint)stateHash);
            Debug.Log("STATE HASH  " + index);
            
            
        }
        
        
        
    }
    
    
}