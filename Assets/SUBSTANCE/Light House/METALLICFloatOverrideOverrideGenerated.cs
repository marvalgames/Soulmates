using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Rendering
{
    [MaterialProperty("_METALLIC")]
    struct METALLICFloatOverride : IComponentData
    {
        public float Value;
    }
}
