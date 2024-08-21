using Unity.Entities;
using UnityEngine;

namespace Sandbox.Player
{
    [System.Serializable]

    public struct PlayerRollComponent : IComponentData
    {


    }


    public class PlayerRollAuthoring : MonoBehaviour

    {
        class SkipMatchupBaker : Baker<PlayerRollAuthoring>
        {
            public override void Bake(PlayerRollAuthoring authoring)
            {
                var e = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);
                AddComponent(e, new PlayerRollComponent());
            }
        }

       
    }
}