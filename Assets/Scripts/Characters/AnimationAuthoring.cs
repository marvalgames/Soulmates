using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

namespace Sandbox.Player
{
    public class AnimationAuthoring : MonoBehaviour
    {
        [HideInInspector] [Header("Bots")] public int NumBots = 10;

        [Header("Prefabs")] public GameObject BotPrefab;
        public GameObject BotAnimatedPrefabGO;

        public float yLocation = 3;
        public bool useTerrainHeight = false;

        public List<CharacterDataClass> CharacterDataObject = new();


        class Baker : Baker<AnimationAuthoring>
        {
            public override void Bake(AnimationAuthoring authoring)
            {
                var entity = GetEntity(authoring, TransformUsageFlags.None);
                var animatedPrefab = authoring.BotAnimatedPrefabGO is not null;

                AddComponent(entity, new CharacterData
                    {
                        NumBots = authoring.NumBots,
                        BotPrefab = GetEntity(authoring.BotPrefab, TransformUsageFlags.Dynamic),
                        HasAnimatedPrefab = animatedPrefab,
                        UseTerrainHeight = authoring.useTerrainHeight,
                        yLocation = authoring.yLocation
                    }
                );

                var buffer = AddBuffer<CharacterDataElement>(entity);
                //var characterDataManagedList = new CharacterDataManaged[authoring.NumBots];

                for (var i = 0; i < authoring.CharacterDataObject.Count; i++)
                {
                    var characterData = authoring.CharacterDataObject[i];
                    var characterDataElement = new CharacterDataElement
                    {
                        //PrefabGroup = i,
                        BotPrefab = GetEntity(characterData.BotPrefab, TransformUsageFlags.Dynamic),
                        NumBots = characterData.numBots,
                        minPosX = characterData.minPosX,
                        maxPosX = characterData.maxPosX,
                        minPosZ = characterData.minPosZ,
                        maxPosZ = characterData.maxPosZ
                    };


                    buffer.Add(characterDataElement);
                }


                var animatedPrefabList = new CharacterDataManaged();
                animatedPrefabList.BotAnimatedPrefabGO = authoring.BotAnimatedPrefabGO; //default
                for (var i = 0; i < authoring.CharacterDataObject.Count; i++)
                {
                    animatedPrefabList.BotAnimatedPrefabList.Add(authoring.CharacterDataObject[i].BotAnimatedPrefab);
                }


                AddComponentObject(entity, animatedPrefabList);

                if (animatedPrefab)
                {
                    var configManaged = new CharacterDataManaged
                    {
                        BotAnimatedPrefabGO = authoring.BotAnimatedPrefabGO
                    };
                    //AddComponentObject(entity, configManaged);
                }
            }
        }
    }

    [System.Serializable]
    public class CharacterDataClass
    {
        public GameObject BotAnimatedPrefab;
        public GameObject BotPrefab;
        [FormerlySerializedAs("NumBots")] public int numBots = 25;
        public float minPosX = -15;
        public float maxPosX = 15;
        public float minPosZ = -895;
        public float maxPosZ = -675;
    }

    [InternalBufferCapacity(16)]
    public struct CharacterDataElement : IBufferElementData
    {
        //public int PrefabGroup;//track index
        public int NumBots;
        public Entity BotPrefab;
        public float minPosX;
        public float maxPosX;
        public float minPosZ;
        public float maxPosZ;
    }

    public struct CharacterData : IComponentData
    {
        public int NumBots;
        public Entity BotPrefab;
        public bool HasAnimatedPrefab;
        public bool UseTerrainHeight;
        public float yLocation;
    }

    public struct CharacterIndexComponent : IComponentData
    {
        public int GroupIndex;
        public int BotIndex;
    }

    public class CharacterDataManaged : IComponentData
    {
        public GameObject BotAnimatedPrefabGO;
        public List<GameObject> BotAnimatedPrefabList = new();
    }

    public class BotAnimation : IComponentData
    {
        public GameObject AnimatedGO; // the GO that is rendered and animated
    }
}