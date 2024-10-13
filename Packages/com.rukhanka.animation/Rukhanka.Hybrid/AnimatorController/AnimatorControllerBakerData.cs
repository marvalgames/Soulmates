using System;
using Unity.Collections.LowLevel.Unsafe;
using Hash128 = Unity.Entities.Hash128;
using FixedStringName = Unity.Collections.FixedString512Bytes;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Entities;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.Hybrid
{
//	RTP - Ready to process
namespace RTP
{ 

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

public struct State: IEquatable<int>
{
	public int hashCode;
	public FixedStringName name;
	public FixedStringName tag;
	public float speed;
	public FixedStringName speedMultiplierParameter;
	public UnsafeList<Transition> transitions;
	public FixedStringName timeParameter;
	public float cycleOffset;
	public FixedStringName cycleOffsetParameter;
	public Motion motion;

	public bool Equals(int o) => o == hashCode;
}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

public struct ChildMotion
{
	public Motion motion;
	public float threshold;
	public float timeScale;
	public FixedStringName directBlendParameterName;
	public float2 position2D;
}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

public struct Motion
{
	public FixedStringName name;
	public MotionBlob.Type type;
	public int animationIndex;
	public BlendTree blendTree;
}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

public struct BlendTree
{
	public FixedStringName name;
	public FixedStringName blendParameterName;
	public FixedStringName blendParameterYName;
	public bool normalizeBlendValues;
	public UnsafeList<ChildMotion> motions;
}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

public struct Transition
{
	public FixedStringName name;
	public float duration;
	public float exitTime;
	public float offset;
	public bool hasExitTime;
	public bool hasFixedDuration;
	public bool soloFlag;
	public bool muteFlag;
	public bool canTransitionToSelf;
	public int targetStateHash;
	public UnsafeList<Condition> conditions;
}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

public struct Condition
{
	public FixedStringName name;
	public FixedStringName paramName;
	public ParameterValue threshold;
	public AnimatorConditionMode conditionMode;
}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

public struct Layer
{
	public FixedStringName name;
	public int defaultStateIndex;
	public float weight;
	public AnimationBlendingMode blendMode;
	public UnsafeList<Transition> anyStateTransitions;
	public UnsafeList<State> states;
	public Hash128 avatarMaskBlobHash;
}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

public struct Parameter: IEquatable<FixedStringName>
{
	public FixedStringName name;
	public ParameterValue defaultValue;
	public ControllerParameterType type;

	public bool Equals(FixedStringName o) => o == name;
}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

public struct Controller
{
	public FixedStringName name;
	public Hash128 hash;
	public UnsafeList<Layer> layers;
	public UnsafeList<Parameter> parameters;
}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

public struct AvatarMask
{
	public FixedStringName name;
	public Hash128 hash;
	public NativeList<FixedStringName> includedBonePaths;
	public uint humanBodyPartsAvatarMask;
}

} // RTP
}

