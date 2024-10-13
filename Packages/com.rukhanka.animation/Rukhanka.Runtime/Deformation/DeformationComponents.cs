#if !RUKHANKA_NO_DEFORMATION_SYSTEM

using Unity.Entities;
using Unity.Mathematics;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka
{
    //  Just a copy of EG SkinMatrix
    public struct SkinMatrix: IBufferElementData
    {
        public float3x4 Value;
    }
    
    //  Just a copy of EG BlendShapeWeight
    public struct BlendShapeWeight : IBufferElementData
    {
        public float Value;
    }
}

#endif