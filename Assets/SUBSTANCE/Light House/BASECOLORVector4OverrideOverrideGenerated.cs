using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Rendering
{
    [MaterialProperty("_BASECOLOR")]
    struct BASECOLORVector4Override : IComponentData
    {
        public float4 Value;
    }
}
