using Unity.Entities;
using Unity.Mathematics;

/////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka
{
public struct BoneVisualizationComponent: IComponentData, IEnableableComponent
{
	public float4 colorTri;
	public float4 colorLines;
	public bool individualConfig;
}
}
