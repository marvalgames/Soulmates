using Unity.Entities;
using UnityEngine;

namespace Managers
{
    public class ManagedComponentsAuthoring : MonoBehaviour //NOT USED???
    {
        public class ManagedComponentsAuthoringBaker : Baker<ManagedComponentsAuthoring>
        {
            public override void Bake(ManagedComponentsAuthoring authoring)
            {
                var e = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(e, new CameraControlsComponent());
            }
        }
    }

}