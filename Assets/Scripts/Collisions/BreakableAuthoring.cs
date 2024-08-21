using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Collisions
{
    public struct BreakableComponent : IComponentData
    {
        int value;
        public float damageAmount;
        public bool enemyDamaged;
        public bool playerDamaged;
        public bool instantiated;
        public bool trigger;
        public float currentTime;
        public float spawnTime;
        public bool destroy;
        public float framesToSkip;//timer instead?
        public int frameSkipCounter;
        public int damageEffectsIndex;
        public int deathBlowEffectsIndex;
        public float gravityFactorAfterBreaking;
        public int groupIndex;
        public bool broken;
        public bool playEffect;
        public int effectIndex;
        public Entity breakerEntity;
        public LocalTransform parentLocalTransform;
        public Entity parentEntity;


    }

    public class BreakableAuthoring : MonoBehaviour
    {

        public float damageAmount = 1;
        public float framesToSkip = 30;//timer instead?
        public int damageEffectsIndex;
        public int deathBlowEffectsIndex;
        public float gravityFactorAfterBreaking = 1;
        public int groupIndex = 1;
        public int effectIndex = 0;
        public Transform parent;

        class BreakableBaker : Baker<BreakableAuthoring>
        {
            public override void Bake(BreakableAuthoring authoring)
            {
                var entity = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);
                //Debug.Log("PARENT " + _parentEntity.Index);

                var transform1 = authoring.transform;
                var position1 = transform1.position;
                var position = new float3(position1.x,
                    position1.y, position1.z);

            
                var breakable = new BreakableComponent
                {
                    damageAmount = authoring.damageAmount,
                    framesToSkip = authoring.framesToSkip,
                    damageEffectsIndex = authoring.damageEffectsIndex,
                    deathBlowEffectsIndex = authoring.deathBlowEffectsIndex,
                    gravityFactorAfterBreaking = authoring.gravityFactorAfterBreaking,
                    groupIndex = authoring.groupIndex,
                    effectIndex = authoring.effectIndex,
                    parentLocalTransform = LocalTransform.FromPosition(position),
                    parentEntity = GetEntity(authoring.parent.gameObject, TransformUsageFlags.Dynamic)


                };

                //Debug.Log("breaking");

                AddComponent(entity, breakable);
            }
        }




    }
}