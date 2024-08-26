using System;
using Sandbox.Player;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Serialization;

namespace Player
{
    [Serializable]
    public class PlayerJumpGameObjectClass : IComponentData
    {
        [FormerlySerializedAs("audioSourcePrefab")] public GameObject audioSourceGo;
        public GameObject vfxSystem;
        public AudioClip clip;
    }
    public class PlayerJumpAuthoring : MonoBehaviour
    {
        [HideInInspector] public float startJumpGravityForce = 9.81f;
        [HideInInspector] public float addedNegativeForce;
        [HideInInspector] public float jumpDownGravityMultiplier = 1.0f;
        [HideInInspector] public float jumpY = 6f;
        [HideInInspector] public float airForce = 500f;
        [SerializeField] private bool disabled;
        [FormerlySerializedAs("audioSourcePrefab")] [FormerlySerializedAs("AudioSourcePrefab")] public GameObject audioSourceGo;
        [FormerlySerializedAs("AudioClip")] public AudioClip audioClip;
        public GameObject vfxPrefab;

    
    
        [Header("Jump Settings")]
        [Range(1, 3)]
        public int jumpPoints;
        public float heightOneFrames = 6;
        [Range(.05f, 5f)]
        public float heightTwoTime = .5f;
        [Range(.1f, 10f)]
        public float heightThreeTime = 1;


        [Header("Double Jump Settings")]
        public bool doubleJump;
        public float doubleHeightOneFrames = 18;
        [Range(.05f, 5f)]
        public float doubleHeightTwoTime = .5f;
        [Range(.1f, 10f)]
        public float doubleHeightThreeTime = 1;


        class PlayerJumpBaker : Baker<PlayerJumpAuthoring>
        {
            public override void Bake(PlayerJumpAuthoring authoring)
            {
                if(authoring.disabled) return;
                if (GetComponent<PlayerRatings>())
                {
                    authoring.startJumpGravityForce = GetComponent<PlayerRatings>().Ratings.startJumpGravityForce;
                    authoring.addedNegativeForce = GetComponent<PlayerRatings>().Ratings.addedNegativeForce;
                    authoring.jumpDownGravityMultiplier = GetComponent<PlayerRatings>().Ratings.jumpDownGravityMultiplier;
                    authoring.jumpY = GetComponent<PlayerRatings>().Ratings.jumpY;
                    authoring.airForce = GetComponent<PlayerRatings>().Ratings.airForce;
                }
            
                var e = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);


                AddComponent
                (
                    e,
                    new PlayerJumpComponent
                    {
                        startJumpGravityForce = authoring.startJumpGravityForce,
                        gameStartJumpGravityForce = authoring.startJumpGravityForce,
                        addedNegativeForce = authoring.addedNegativeForce,
                        jumpDownGravityMultiplier = authoring.jumpDownGravityMultiplier,
                        jumpY = authoring.jumpY,
                        airForce = authoring.airForce,
                        jumpPoints = authoring.jumpPoints,
                        heightOneFrames = authoring.heightOneFrames,
                        JumpStartFrames = authoring.heightOneFrames,
                        heightTwoTime = authoring.heightTwoTime,
                        JumpStartHeightTwoTime = authoring.heightTwoTime,
                        heightThreeTime = authoring.heightThreeTime,
                        JumpStartHeightThreeTime = authoring.heightThreeTime,
                        doubleHeightOneFrames = authoring.doubleHeightOneFrames,
                        doubleHeightTwoTime = authoring.doubleHeightTwoTime,
                        doubleHeightThreeTime = authoring.doubleHeightThreeTime,
                        doubleJump = authoring.doubleJump
                    }
                ); ; ;
            
                AddComponentObject(e, new PlayerJumpGameObjectClass
                {
                    clip = authoring.audioClip,
                    vfxSystem = authoring.vfxPrefab,
                    audioSourceGo = authoring.audioSourceGo
                    
                });
            
         
            
            }
        }


   
    }
}