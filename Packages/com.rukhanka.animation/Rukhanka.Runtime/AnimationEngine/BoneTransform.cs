using Unity.Mathematics;
using Unity.Transforms;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka
{
public struct BoneTransform
{
	public float3 pos;
	public quaternion rot;
	public float3 scale;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	public BoneTransform(LocalTransform lt)
	{
		pos = lt.Position;
		rot = lt.Rotation;
		scale = lt.Scale;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	public static BoneTransform Identity()
	{
		return new BoneTransform() { pos = 0, rot = quaternion.identity, scale = 1 };
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	public float4x4 ToFloat4x4()
	{
		return float4x4.TRS(pos, rot, scale);
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	//	Multiply child with parent
	public static BoneTransform Multiply(in BoneTransform parent, in BoneTransform child)
	{
		var rv = new BoneTransform();
		rv.pos = math.mul(parent.rot, child.pos * parent.scale) + parent.pos;
		rv.rot = math.mul(parent.rot, child.rot);
		rv.scale = parent.scale * child.scale;
		return rv;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	public static BoneTransform Inverse(in BoneTransform bt)
	{
		var rv = new BoneTransform();
		rv.rot = math.inverse(bt.rot);
		rv.scale = math.rcp(bt.scale);
		rv.pos = math.mul(rv.rot, -bt.pos * rv.scale);
		return rv;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	public static BoneTransform Scale(in BoneTransform bt, float3 scale)
	{
		var rv = new BoneTransform()
		{
			pos = bt.pos * scale.x,
			rot = bt.rot.value * scale.y,
			scale = bt.scale * scale.z,
		};
		return rv;
	}
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	public static BoneTransform Lerp(in BoneTransform bt0, in BoneTransform bt1, float f)
	{
		var rv = new BoneTransform()
		{
			pos = math.lerp(bt0.pos, bt1.pos, f),
			rot = math.slerp(bt0.rot.value, bt1.rot.value, f),
			scale = math.lerp(bt0.scale, bt1.scale, f)
		};
		return rv;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	public LocalTransform ToLocalTransformComponent() => new LocalTransform() { Position = pos, Rotation = rot, Scale = scale.x };
}
}
