using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Collisions
{
    public enum TriggerType
    {
        None = 0,
        Body = 1,
        Head = 2,
        Base = 3, //chest for player
        LeftHand = 4,
        RightHand = 5,
        LeftFoot = 6,
        RightFoot = 7,
        Ammo = 8,
        Blocks = 9,
        Ground = 10,
        Lever = 11,
        Melee = 12,
        Particle = 13,
        Breakable = 14,
        Tail = 15,
        Player = 16,
        Stairs = 17,
        Platform = 18
    }

    public struct LevelCompleteRemove : IComponentData
    {
        public int levelCompleteIndex;
    }

    public struct TriggerComponent : IComponentData
    {
        public int key;
        public int Type;
        public int index;
        public int CurrentFrame;

        public bool TriggerChecked;

        //parent of trigger ie bone 
        //if trigger is bullet then just returns bullet not shooter
        //use ammo component for shooter (owner)
        public Entity Entity;
        public Entity ParentEntity;
        public Entity TriggeredVfxEntity;
        public Entity SpawnedVfxEntity;
        public bool Hit;
        public bool Active;
        public int VfxSpawned;
    }

    [System.Serializable]
    public class VfxClass : IComponentData
    {
        public AudioSource audioSource;
    }


    public struct TriggeredComponentTag : IComponentData
    {
    }


    public class Trigger : MonoBehaviour
    {
        //Can't because of WEBGL
        [Tooltip("Level to Remove Entity and corresponding GameObject Mesh  (0 never removes or creates Component Tag)  ")]
        public int levelCompleteIndex;

        public bool parentActor;

        public TriggerType Type;
        [SerializeField] private int index;
        public GameObject ParentGameObject;
        public bool deParent;
        public GameObject triggerVfxPrefab;
        public AudioSource triggerAudioSource;


        public class TriggerBaker : Baker<Trigger>
        {
            public override void Bake(Trigger authoring)
            {
                if (authoring.deParent)
                {
                    authoring.transform.parent = null;
                }


                var e = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);
                var parentEntity = e;

                var parentGameObject = authoring.ParentGameObject;

                var parent = authoring.transform.parent;

                if (parentGameObject == null)
                {
                    if (parent != null)
                    {
                        parentEntity = GetEntity(parent, TransformUsageFlags.Dynamic);
                    }
                }
                else
                {
                    parentEntity = GetEntity(parentGameObject, TransformUsageFlags.Dynamic);
                }
                //Debug.Log("PARENT " + parentEntity);

                var vfxEntity = GetEntity(authoring.triggerVfxPrefab, TransformUsageFlags.Dynamic);


                var trigger = new TriggerComponent
                {
                    Type = (int)authoring.Type,
                    Entity = e,
                    ParentEntity = parentEntity,
                    TriggeredVfxEntity = vfxEntity,
                    CurrentFrame = 0,
                    index = authoring.index,
                    Hit = false,
                    Active = true,
                };

                AddComponent(e, trigger);
                if (authoring.parentActor)
                {
                    AddComponent(e, new Parent
                        {
                            Value = parentEntity
                        }
                    );
                }

                if (authoring.levelCompleteIndex > 0)
                {
                    AddComponent(e, new LevelCompleteRemove { levelCompleteIndex = authoring.levelCompleteIndex });
                }




            }
        }
    }
}