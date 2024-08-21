using Unity.Entities;
using UnityEngine;

namespace Enemy
{
    public class EnemyJobsAuthoring : MonoBehaviour
    {
        public class EnemyJobsAuthoringBaker : Baker<EnemyJobsAuthoring>
        {
            public override void Bake(EnemyJobsAuthoring authoring)
            {
                var e = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(e, new EnemyJobsComponentData());
            }
        }
    }

    public struct EnemyJobsComponentData : IComponentData
    {
    }
}