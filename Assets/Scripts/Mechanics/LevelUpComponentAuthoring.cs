using Unity.Entities;
using UnityEngine;


public struct LevelUpMechanicComponent : IComponentData
{
    public bool active;
    public float multiplier;

}


public class LevelUpComponentAuthoring: MonoBehaviour
{
    public bool active = true;
    public float multiplier = 1;

    class LevelUpAuthoringBaker : Baker<LevelUpComponentAuthoring>
    {
        public override void Bake(LevelUpComponentAuthoring authoring)
        {
            var e = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);

            AddComponent
                (
                    e,
                    new LevelUpMechanicComponent { active = authoring.active, multiplier = authoring.multiplier }
                );
        }
    }


}
