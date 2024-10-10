using System;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Sandbox.Player
{
    public class ActorEntityTracker : MonoBehaviour
    {
        //[HideInInspector] 
        public AnimationStage animationStageTracker = AnimationStage.Enter;
        public int debugCounter = 0;
        public string defaultStateName;
        //public bool isAnimationUpdatedCurrentFrame;
    }
}