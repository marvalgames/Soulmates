using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Rendering
{
    [MaterialProperty("_SPECROUGH")]
    struct SPECROUGHFloatOverride : IComponentData
    {
        public float Value;
    }
}
