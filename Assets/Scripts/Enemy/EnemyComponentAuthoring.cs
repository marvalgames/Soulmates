using System;
using Collisions;
using Sandbox.Player;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

[System.Serializable]
public class EnemyClass : IComponentData
{
    public GameObject go;
}

[System.Serializable]
public struct EnemyComponent : IComponentData
{
    [System.NonSerialized] public Entity e;

    public bool invincible;

    public float3 startPosition;

    //public bool humanoid;
    public int areaIndex;
}

public struct EnemiesAttackComponent : IComponentData, IEnableableComponent
{
    public bool enemiesAttack;
}


public class EnemyComponentAuthoring : MonoBehaviour
{
    // public Entity enemyEntity;
    // public EntityManager manager;

    public bool enemiesAttack;

    public bool isNavMeshAgent = true;

    [SerializeField] private bool checkLossCondition;

    [SerializeField] bool checkWinCondition;

    [SerializeField] bool invincible;

    [SerializeField] int saveIndex;

    [SerializeField] bool paused = false;

    [SerializeField] int areaIndex;


    void LateUpdate()
    {
        // if (manager == default) return;
        // if (!manager.HasComponent(enemyEntity, typeof(LocalTransform))) return;
        //
        // manager.SetComponentData(enemyEntity, new LocalTransform { Value = transform.position });
    }

    class EnemyComponentBaker : Baker<EnemyComponentAuthoring>
    {
        public override void Bake(EnemyComponentAuthoring authoring)
        {
            var e = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);
            AddComponent(e,
                new EnemyComponent
                {
                    e = e,
                    invincible = authoring.invincible,
                    startPosition = authoring.transform.position
                }
            );
            AddComponent(e, new EnemiesAttackComponent  {enemiesAttack = authoring.enemiesAttack} );
            if (authoring.isNavMeshAgent)
            {
                AddComponent(e, new NavMeshAgentComponent());
            }
            SetComponentEnabled<EnemiesAttackComponent>(e, authoring.enemiesAttack);
            
            if (authoring.paused == true)
            {
                AddComponent(e, new Pause());
            }


            AddComponent(e, new StatsComponent()
                {
                    shotsFired = 0,
                    shotsLanded = 0
                }
            );


            AddComponent(e, new SkillTreeComponent()
                {
                    e = e,
                    availablePoints = 0,
                    SpeedPts = 0,
                    PowerPts = 0,
                    ChinPts = 0,
                    baseSpeed = 0,
                    CurrentLevel = 1,
                    CurrentLevelXp = 0,
                    PointsNextLevel = 10
                }
            );


            AddComponent(e, new WinnerComponent
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
                    checkWinCondition = authoring.checkWinCondition,
                    areaIndex = authoring.areaIndex
                }
            );


            AddComponent(e, new DeadComponent
                {
                    tag = 2,
                    isDead = false,
                    checkLossCondition = authoring.checkLossCondition
                }
            );
            var a = authoring.name;
            var b = string.Empty;
            var val = 0;

            for (var i = 0; i < a.Length; i++)
            {
                if (Char.IsDigit(a[i]))
                    b += a[i];
            }

            if (b.Length > 0)
                val = int.Parse(b);

            //int index = Int32.Parse(str);//fix
            var index = val;

            //Debug.Log("go " + a + " val " + index);

            AddComponent(e, new CharacterSaveComponent { saveIndex = index });

            AddComponent(e, new CheckedComponent());

            AddComponent(e,
                new EnemyStateComponent { MoveState = MoveStates.Default, CombatState = CombatStates.Default });

            //AddComponent(new EnemyClass(){go = authoring.gameObject});
        }
    }
}