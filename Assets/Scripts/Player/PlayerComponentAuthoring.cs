using Collisions;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Sandbox.Player
{
    [System.Serializable]
    public struct StatsComponent : IComponentData
    {
        public int shotsFired;
        public int shotsLanded;

    }

    [System.Serializable]
    public struct TotalCurrencyComponent : IComponentData
    {
        public float currency;

    }

    [System.Serializable]
    public struct SkillTreeComponent : IComponentData
    {

        [System.NonSerialized]
        public Entity e;
        public int CurrentLevel;
        public float CurrentLevelXp;
        public int PointsNextLevel;

        public int availablePoints;
        public int SpeedPts;
        public int PowerPts;
        public int ChinPts;
        public float TotalPoints;

        public float baseSpeed;


    }

  



    [System.Serializable]
    public struct PlayerComponent : IComponentData
    {
        public int index;//1 is p1 2 is p2 etc 1 is required for skill tree group
        public int keys;
        public int tag;
        public float speed;
        public float power;
        public float maxHealth;
        public bool threeD;
        public float3 startPosition;

    }




    public class PlayerComponentAuthoring : MonoBehaviour
    {
     
        
        public int index = 0;

        [SerializeField]
        private bool checkWinCondition = true;

        [SerializeField]
        private bool checkLossCondition = true;


        public bool threeD;
        public int skillTreePointsToNextLevel = 10;
        public float lookAtDistance = 5;


        [SerializeField]
        bool paused = false;


        public class PlayerComponentBaker : Baker<PlayerComponentAuthoring>
        {
            public override void Bake(PlayerComponentAuthoring authoring)
            {
                var e = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);
                //Debug.Log("add level complete component e " + e.Index);
              
                AddComponent(e, new EnemiesAttackComponent());
                

                AddComponent(e, new PlayerComponent
                    {
                        
                        index = authoring.index,
                        threeD = authoring.threeD,
                        startPosition = authoring.transform.position
                    }

                );


                if (authoring.paused == true)
                {
                    AddComponent(e, new Pause());
                }


                AddComponent(e,
                    new WinnerComponent
                    {
                        active = true,
                        goalCounter = 0,
                        goalCounterTarget = 0, //ie how many players you have to save - usually zero
                        targetReached = false,
                        endGameReached = false,
                        checkWinCondition = authoring.checkWinCondition
                    }
                );

                AddComponent(e,
                    new LevelCompleteComponent
                    {
                        active = true,
                        targetReached = false,
                        checkWinCondition = authoring.checkWinCondition
                    }
                );

                AddComponent(e, new DeadComponent
                    {
                        tag = 1,
                        isDead = false,
                        checkLossCondition = authoring.checkLossCondition

                    }
                );

                AddComponent(e, new SkillTreeComponent()
                    {
                        //e = entity,
                        
                        availablePoints = 0,
                        TotalPoints = 0,
                        SpeedPts = 0,
                        PowerPts = 0,
                        ChinPts = 0,
                        baseSpeed = 0,
                        CurrentLevel = 1,
                        CurrentLevelXp = 0,
                        PointsNextLevel = authoring.skillTreePointsToNextLevel

                    }

                );

                AddComponent(e, new StatsComponent()
                    {
                        shotsFired = 0,
                        shotsLanded = 0
                    }
                );

                AddComponent(e, new CheckedComponent());

                AddComponent(e, 
                    new MatchupComponent
                    {
                        lookAtDistance = authoring.lookAtDistance

                    });

                AddComponent(e, new TotalCurrencyComponent()
                    {
                        currency = 0
                    }
                );
            }
            //Debug.Log("player convert ");



        }


    }
}