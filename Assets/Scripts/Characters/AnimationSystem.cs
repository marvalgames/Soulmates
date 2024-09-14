using Collisions;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.TextCore.Text;

namespace Sandbox.Player
{
    [UpdateAfter(typeof(RaycastHeightSystem))]
    public partial struct AnimationSystem : ISystem
    {
        private bool isInitialized;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PhysicsWorldSingleton>();
            state.RequireForUpdate<CharacterData>();
            state.RequireForUpdate<Bot>();
        }

        // Because this update accesses managed objects, it cannot be Burst compiled,
        // so we do not add the [BurstCompile] attribute.
        public void OnUpdate(ref SystemState state)
        {
            if (!isInitialized)
            {
                isInitialized = true;

                var configEntity = SystemAPI.GetSingletonEntity<CharacterData>();
                if (state.EntityManager.HasComponent<CharacterDataManaged>(configEntity))
                {
                    var characterDataBuffer = SystemAPI.GetBufferLookup<CharacterDataElement>(true);
                    var characterData = SystemAPI.GetSingleton<CharacterData>();
                    var configManaged = state.EntityManager.GetComponentObject<CharacterDataManaged>(configEntity);
                    //var group = characterDataBuffer[configEntity][0].PrefabGroup;

                    var ecb = new EntityCommandBuffer(Allocator.Temp);

                    foreach (var (transform, characterIndex, entity) in
                             SystemAPI.Query<RefRO<LocalTransform>, RefRO<CharacterIndexComponent>>()
                                 .WithAll<Bot>()
                                 .WithEntityAccess())
                    {
                        var animatedPrefab = configManaged.BotAnimatedPrefabList[characterIndex.ValueRO.GroupIndex];
                        Debug.Log("animated prefab " + animatedPrefab + " " + characterIndex.ValueRO.GroupIndex);
                        var botAnimation = new BotAnimation();
                        var go = GameObject.Instantiate(animatedPrefab);

                        botAnimation.AnimatedGO = go;
                        go.transform.localPosition = (Vector3)transform.ValueRO.Position;
                        //Debug.Log("Pos Y " + go.transform.localPosition.y);
                        ecb.AddComponent(entity, botAnimation);

                        // disable rendering
                        ecb.RemoveComponent<MaterialMeshInfo>(entity);
                    }

                    ecb.Playback(state.EntityManager);
                }
            }

            //var isMovingId = Animator.StringToHash("IsMoving");
            var vertical = Animator.StringToHash("Vertical");


            foreach (var (bot, transform, botAnimation, entity) in
                     SystemAPI.Query<RefRO<Bot>, RefRO<LocalTransform>, BotAnimation>().WithEntityAccess())
            {
                var pos = (Vector3)transform.ValueRO.Position;
                //pos.y = 0;
                botAnimation.AnimatedGO.transform.position = pos;
                botAnimation.AnimatedGO.transform.rotation = (Quaternion)transform.ValueRO.Rotation;
                
                var animator = botAnimation.AnimatedGO.GetComponent<Animator>();
                //animator.SetBool(isMovingId, bot.ValueRO.IsMoving());
                animator.SetFloat(vertical, 1);
            }
        }

       
        
    }
}