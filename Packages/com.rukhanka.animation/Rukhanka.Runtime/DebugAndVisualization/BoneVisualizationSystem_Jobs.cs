#if !RUKHANKA_NO_DEBUG_DRAWER
using Rukhanka.DebugDrawer;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

/////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka
{
partial class BoneVisualizationSystem
{
[BurstCompile]
partial struct RenderBonesJob: IJobEntity
{
	[ReadOnly]
	public NativeList<BoneTransform> bonePoses;
	[ReadOnly]
	public NativeParallelHashMap<Entity, RuntimeAnimationData.AnimatedEntityBoneDataProps> entityToDataOffsetMap;

    public Drawer drawer;
    
/////////////////////////////////////////////////////////////////////////////////

    public void Execute(Entity e, in RigDefinitionComponent rd, in BoneVisualizationComponent bvc)
    {
        var bt = RuntimeAnimationData.GetAnimationDataForRigRO(bonePoses, entityToDataOffsetMap, e);

        var len = bt.Length;
        
        for (int l = rd.rigBlob.Value.rootBoneIndex; l < len; ++l)
        {
            ref var rb = ref rd.rigBlob.Value.bones[l];

            if (rb.parentBoneIndex < 0)
                continue;

            var bonePos0 = bt[l].pos;
            var bonePos1 = bt[rb.parentBoneIndex].pos;

            if (math.any(math.abs(bonePos0 - bonePos1)))
            {
                var colorTri = Drawer.ColorToUINT(bvc.colorTri);
                var colorLines = Drawer.ColorToUINT(bvc.colorLines);
                drawer.DrawBoneMesh(bt[l].pos, bt[rb.parentBoneIndex].pos, colorTri, colorLines);
            }
        }
    }
}
}
}
#endif
