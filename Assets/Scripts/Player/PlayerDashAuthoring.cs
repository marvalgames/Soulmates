using Unity.Entities;
using UnityEngine;
using Collider = Unity.Physics.Collider;

namespace Sandbox.Player
{
    

    public struct PlayerDashComponent : IComponentData
    {
        public bool active;
        public int uses;
        public float power;
        public float dashTime;
        public float DashTimeTicker;
        public float delayTime;
        public float DelayTimeTicker;
        public float invincibleStart;
        public float invincibleEnd;
        //public PhysicsCollider Collider;
        public BlobAssetReference<Collider> box;
        public bool Invincible;
        public bool InDash;

    }

    public struct Invincible : IComponentData
    {
        public int Value;
    }

    public class PlayerDashGameObjectClass : IComponentData
    {
        //public BlobAssetReference<Unity.Physics.Collider> box;
        public GameObject audioSource;
        public AudioClip audioClip;
        public GameObject vfxPrefab;
        //public Transform transform;
    }

    public class PlayerDashAuthoring : MonoBehaviour

    {
        //public BlobAssetReference<Unity.Physics.Collider> box;
        public float power = 10;
        public float dashTime = 1;
        public float delayTime = .5f;
        public float invincibleStart = .1f;
        public float invincibleEnd = 1f;
        public int uses = 9999;
        public bool active = true;

        public GameObject audioSource;
        public AudioClip clip;
        public GameObject vfxPrefab;



        class PlayerDashBaker : Baker<PlayerDashAuthoring>
        {
            public override void Bake(PlayerDashAuthoring authoring)
            {
                var e = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);

                AddComponent(e, new PlayerDashComponent
                    {
                        active = authoring.active,
                        power = authoring.power,
                        uses = authoring.uses,
                        dashTime = authoring.dashTime,
                        delayTime = authoring.delayTime,
                        invincibleStart = authoring.invincibleStart,
                        invincibleEnd = authoring.invincibleEnd
                    }
                );
                
                
                AddComponentObject(e, 
                    new PlayerDashGameObjectClass
                    {
                        audioSource = authoring.audioSource,
                        audioClip = authoring.clip,
                        vfxPrefab = authoring.vfxPrefab
                        //transform = authoring.transform
                    } );

                
            }
        }



      
    }
}