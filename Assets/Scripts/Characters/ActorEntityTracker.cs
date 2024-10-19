using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using UnityEngine;

namespace Sandbox.Player
{
    public class ActorEntityTracker : MonoBehaviour
    {
        //[HideInInspector] 
        public AnimationStage animationStageTracker = AnimationStage.Enter;
        public int debugCounter = 0;
        public string defaultStateName;
        public Entity linkedEntity = Entity.Null;
        public EntityManager manager;

        public bool LockX, LockY, LockZ;
        //public bool isAnimationUpdatedCurrentFrame;
        
        void Update()
        {
       
            if(linkedEntity == Entity.Null) return;
            //Debug.Log("lock " + linkedEntity);
            if (manager.HasComponent<PhysicsMass>(linkedEntity))
            {
                var mass = manager.GetComponentData<PhysicsMass>(linkedEntity);
                //mass.InverseMass = .01f;
                mass.InverseInertia[0] = LockX ? .0f: mass.InverseInertia[0];
                mass.InverseInertia[1] = LockY ? .0f: mass.InverseInertia[1];
                mass.InverseInertia[2] = LockZ ? .0f : mass.InverseInertia[2]; 
                manager.SetComponentData(linkedEntity, mass);
            }


        }

        
        
        
    }
    
    
   
    
    
    
}