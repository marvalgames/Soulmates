#if UNITY_EDITOR

using System.Collections.Generic;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.Hybrid
{
public partial class AnimationClipBaker
{
	static Dictionary<string, ParsedCurveBinding> humanoidMappingTable;
	static Dictionary<string, string> humanoidMuscleNameFromCurveProperty;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	static AnimationClipBaker()
	{
		humanoidMappingTable = new ()
		{
			//	--- Head ---
			//	Neck
			{ "Neck Nod Down-Up",				new ParsedCurveBinding() { boneName = "Neck", channelIndex = 2, bindingType = BindingType.HumanMuscle }},
			{ "Neck Tilt Left-Right",			new ParsedCurveBinding() { boneName = "Neck", channelIndex = 1, bindingType = BindingType.HumanMuscle }},
			{ "Neck Turn Left-Right",			new ParsedCurveBinding() { boneName = "Neck", channelIndex = 0, bindingType = BindingType.HumanMuscle }},
			//	Head
			{ "Head Nod Down-Up",				new ParsedCurveBinding() { boneName = "Head", channelIndex = 2, bindingType = BindingType.HumanMuscle }},
			{ "Head Tilt Left-Right",			new ParsedCurveBinding() { boneName = "Head", channelIndex = 1, bindingType = BindingType.HumanMuscle }},
			{ "Head Turn Left-Right",			new ParsedCurveBinding() { boneName = "Head", channelIndex = 0, bindingType = BindingType.HumanMuscle }},
			//	Left Eye
			{ "Left Eye Down-Up",				new ParsedCurveBinding() { boneName = "LeftEye", channelIndex = 2, bindingType = BindingType.HumanMuscle }},
			{ "Left Eye In-Out",				new ParsedCurveBinding() { boneName = "LeftEye", channelIndex = 1, bindingType = BindingType.HumanMuscle }},
			//	Right Eye
			{ "Right Eye Down-Up",				new ParsedCurveBinding() { boneName = "RightEye", channelIndex = 2, bindingType = BindingType.HumanMuscle }},
			{ "Right Eye In-Out",				new ParsedCurveBinding() { boneName = "RightEye", channelIndex = 1, bindingType = BindingType.HumanMuscle }},
			//	Jaw
			{ "Jaw Close",						new ParsedCurveBinding() { boneName = "Jaw", channelIndex = 2, bindingType = BindingType.HumanMuscle }},
			{ "Jaw Left-Right",					new ParsedCurveBinding() { boneName = "Jaw", channelIndex = 1, bindingType = BindingType.HumanMuscle }},

			//	--- Body ---
			//	Spine
			{ "Spine Front-Back",				new ParsedCurveBinding() { boneName = "Spine", channelIndex = 2, bindingType = BindingType.HumanMuscle }},
			{ "Spine Left-Right",				new ParsedCurveBinding() { boneName = "Spine", channelIndex = 1, bindingType = BindingType.HumanMuscle }},
			{ "Spine Twist Left-Right",			new ParsedCurveBinding() { boneName = "Spine", channelIndex = 0, bindingType = BindingType.HumanMuscle }},
			//	Chest
			{ "Chest Front-Back",				new ParsedCurveBinding() { boneName = "Chest", channelIndex = 2, bindingType = BindingType.HumanMuscle }},
			{ "Chest Left-Right",				new ParsedCurveBinding() { boneName = "Chest", channelIndex = 1, bindingType = BindingType.HumanMuscle }},
			{ "Chest Twist Left-Right",			new ParsedCurveBinding() { boneName = "Chest", channelIndex = 0, bindingType = BindingType.HumanMuscle }},
			//	UpperChest
			{ "UpperChest Front-Back",			new ParsedCurveBinding() { boneName = "UpperChest", channelIndex = 2, bindingType = BindingType.HumanMuscle }},
			{ "UpperChest Left-Right",			new ParsedCurveBinding() { boneName = "UpperChest", channelIndex = 1, bindingType = BindingType.HumanMuscle }},
			{ "UpperChest Twist Left-Right",	new ParsedCurveBinding() { boneName = "UpperChest", channelIndex = 0, bindingType = BindingType.HumanMuscle }},

			//	--- Left Arm ---
			//	LeftShoulder
			{ "Left Shoulder Down-Up",			new ParsedCurveBinding() { boneName = "LeftShoulder", channelIndex = 2, bindingType = BindingType.HumanMuscle }},
			{ "Left Shoulder Front-Back",		new ParsedCurveBinding() { boneName = "LeftShoulder", channelIndex = 1, bindingType = BindingType.HumanMuscle }},
			//	LeftUpperArm
			{ "Left Arm Down-Up",				new ParsedCurveBinding() { boneName = "LeftUpperArm", channelIndex = 2, bindingType = BindingType.HumanMuscle }},
			{ "Left Arm Front-Back",			new ParsedCurveBinding() { boneName = "LeftUpperArm", channelIndex = 1, bindingType = BindingType.HumanMuscle }},
			{ "Left Arm Twist In-Out",			new ParsedCurveBinding() { boneName = "LeftUpperArm", channelIndex = 0, bindingType = BindingType.HumanMuscle }},
			//	LeftLowerArm
			{ "Left Forearm Stretch",			new ParsedCurveBinding() { boneName = "LeftLowerArm", channelIndex = 2, bindingType = BindingType.HumanMuscle }},
			{ "Left Forearm Twist In-Out",		new ParsedCurveBinding() { boneName = "LeftLowerArm", channelIndex = 0, bindingType = BindingType.HumanMuscle }},
			//	LeftHand
			{ "Left Hand Down-Up",				new ParsedCurveBinding() { boneName = "LeftHand", channelIndex = 2, bindingType = BindingType.HumanMuscle }},
			{ "Left Hand In-Out",				new ParsedCurveBinding() { boneName = "LeftHand", channelIndex = 1, bindingType = BindingType.HumanMuscle }},

			//	--- Left Hand ---
			//	Thumb 1
			{ "LeftHand.Thumb.1 Stretched",		new ParsedCurveBinding() { boneName = "Left Thumb Proximal", channelIndex = 2, bindingType = BindingType.HumanMuscle }},
			{ "LeftHand.Thumb Spread",			new ParsedCurveBinding() { boneName = "Left Thumb Proximal", channelIndex = 1, bindingType = BindingType.HumanMuscle }},
			//	Thumb 2
			{ "LeftHand.Thumb.2 Stretched",		new ParsedCurveBinding() { boneName = "Left Thumb Intermediate", channelIndex = 2, bindingType = BindingType.HumanMuscle }},
			//	Thumb 3
			{ "LeftHand.Thumb.3 Stretched",		new ParsedCurveBinding() { boneName = "Left Thumb Distal", channelIndex = 2, bindingType = BindingType.HumanMuscle }},
			//	Index 1
			{ "LeftHand.Index.1 Stretched",		new ParsedCurveBinding() { boneName = "Left Index Proximal", channelIndex = 2, bindingType = BindingType.HumanMuscle }},
			{ "LeftHand.Index.Spread",			new ParsedCurveBinding() { boneName = "Left Index Proximal", channelIndex = 1, bindingType = BindingType.HumanMuscle }},
			//	Index 2
			{ "LeftHand.Index.2 Stretched",		new ParsedCurveBinding() { boneName = "Left Index Intermediate", channelIndex = 2, bindingType = BindingType.HumanMuscle }},
			//	Index 3
			{ "LeftHand.Index.3 Stretched",		new ParsedCurveBinding() { boneName = "Left Index Distal", channelIndex = 2, bindingType = BindingType.HumanMuscle }},
			//	Middle 1
			{ "LeftHand.Middle.1 Stretched",	new ParsedCurveBinding() { boneName = "Left Middle Proximal", channelIndex = 2, bindingType = BindingType.HumanMuscle }},
			{ "LeftHand.Middle.Spread",			new ParsedCurveBinding() { boneName = "Left Middle Proximal", channelIndex = 1, bindingType = BindingType.HumanMuscle }},
			//	Middle 2
			{ "LeftHand.Middle.2 Stretched",	new ParsedCurveBinding() { boneName = "Left Middle Intermediate", channelIndex = 2, bindingType = BindingType.HumanMuscle }},
			//	Middle 3
			{ "LeftHand.Middle.3 Stretched",	new ParsedCurveBinding() { boneName = "Left Middle Distal", channelIndex = 2, bindingType = BindingType.HumanMuscle }},
			//	Ring 1
			{ "LeftHand.Ring.1 Stretched",		new ParsedCurveBinding() { boneName = "Left Ring Proximal", channelIndex = 2, bindingType = BindingType.HumanMuscle }},
			{ "LeftHand.Ring.Spread",			new ParsedCurveBinding() { boneName = "Left Ring Proximal", channelIndex = 1, bindingType = BindingType.HumanMuscle }},
			//	Ring 2
			{ "LeftHand.Ring.2 Stretched",		new ParsedCurveBinding() { boneName = "Left Ring Intermediate", channelIndex = 2, bindingType = BindingType.HumanMuscle }},
			//	Ring 3
			{ "LeftHand.Ring.3 Stretched",		new ParsedCurveBinding() { boneName = "Left Ring Distal", channelIndex = 2, bindingType = BindingType.HumanMuscle }},
			//	Little 1
			{ "LeftHand.Little.1 Stretched",	new ParsedCurveBinding() { boneName = "Left Little Proximal", channelIndex = 2, bindingType = BindingType.HumanMuscle }},
			{ "LeftHand.Little.Spread",			new ParsedCurveBinding() { boneName = "Left Little Proximal", channelIndex = 1, bindingType = BindingType.HumanMuscle }},
			//	Little 2
			{ "LeftHand.Little.2 Stretched",	new ParsedCurveBinding() { boneName = "Left Little Intermediate", channelIndex = 2, bindingType = BindingType.HumanMuscle }},
			//	Little 3
			{ "LeftHand.Little.3 Stretched",	new ParsedCurveBinding() { boneName = "Left Little Distal", channelIndex = 2, bindingType = BindingType.HumanMuscle }},

			//	--- Right Arm ---
			//	RightShoulder
			{ "Right Shoulder Down-Up",			new ParsedCurveBinding() { boneName = "RightShoulder", channelIndex = 2, bindingType = BindingType.HumanMuscle }},
			{ "Right Shoulder Front-Back",		new ParsedCurveBinding() { boneName = "RightShoulder", channelIndex = 1, bindingType = BindingType.HumanMuscle }},
			//	RightUpperArm
			{ "Right Arm Down-Up",				new ParsedCurveBinding() { boneName = "RightUpperArm", channelIndex = 2, bindingType = BindingType.HumanMuscle }},
			{ "Right Arm Front-Back",			new ParsedCurveBinding() { boneName = "RightUpperArm", channelIndex = 1, bindingType = BindingType.HumanMuscle }},
			{ "Right Arm Twist In-Out",			new ParsedCurveBinding() { boneName = "RightUpperArm", channelIndex = 0, bindingType = BindingType.HumanMuscle }},
			//	RightLowerArm
			{ "Right Forearm Stretch",			new ParsedCurveBinding() { boneName = "RightLowerArm", channelIndex = 2, bindingType = BindingType.HumanMuscle }},
			{ "Right Forearm Twist In-Out",		new ParsedCurveBinding() { boneName = "RightLowerArm", channelIndex = 0, bindingType = BindingType.HumanMuscle }},
			//	RightHand
			{ "Right Hand Down-Up",				new ParsedCurveBinding() { boneName = "RightHand", channelIndex = 2, bindingType = BindingType.HumanMuscle }},
			{ "Right Hand In-Out",				new ParsedCurveBinding() { boneName = "RightHand", channelIndex = 1, bindingType = BindingType.HumanMuscle }},

			//	--- Right Hand ---
			//	Thumb 1
			{ "RightHand.Thumb.1 Stretched",	new ParsedCurveBinding() { boneName = "Right Thumb Proximal", channelIndex = 2, bindingType = BindingType.HumanMuscle }},
			{ "RightHand.Thumb Spread",			new ParsedCurveBinding() { boneName = "Right Thumb Proximal", channelIndex = 1, bindingType = BindingType.HumanMuscle }},
			//	Thumb 2
			{ "RightHand.Thumb.2 Stretched",	new ParsedCurveBinding() { boneName = "Right Thumb Intermediate", channelIndex = 2, bindingType = BindingType.HumanMuscle }},
			//	Thumb 3
			{ "RightHand.Thumb.3 Stretched",	new ParsedCurveBinding() { boneName = "Right Thumb Distal", channelIndex = 2, bindingType = BindingType.HumanMuscle }},
			//	Index 1
			{ "RightHand.Index.1 Stretched",	new ParsedCurveBinding() { boneName = "Right Index Proximal", channelIndex = 2, bindingType = BindingType.HumanMuscle }},
			{ "RightHand.Index.Spread",			new ParsedCurveBinding() { boneName = "Right Index Proximal", channelIndex = 1, bindingType = BindingType.HumanMuscle }},
			//	Index 2
			{ "RightHand.Index.2 Stretched",	new ParsedCurveBinding() { boneName = "Right Index Intermediate", channelIndex = 2, bindingType = BindingType.HumanMuscle }},
			//	Index 3
			{ "RightHand.Index.3 Stretched",	new ParsedCurveBinding() { boneName = "Right Index Distal", channelIndex = 2, bindingType = BindingType.HumanMuscle }},
			//	Middle 1
			{ "RightHand.Middle.1 Stretched",	new ParsedCurveBinding() { boneName = "Right Middle Proximal", channelIndex = 2, bindingType = BindingType.HumanMuscle }},
			{ "RightHand.Middle.Spread",		new ParsedCurveBinding() { boneName = "Right Middle Proximal", channelIndex = 1, bindingType = BindingType.HumanMuscle }},
			//	Middle 2
			{ "RightHand.Middle.2 Stretched",	new ParsedCurveBinding() { boneName = "Right Middle Intermediate", channelIndex = 2, bindingType = BindingType.HumanMuscle }},
			//	Middle 3
			{ "RightHand.Middle.3 Stretched",	new ParsedCurveBinding() { boneName = "Right Middle Distal", channelIndex = 2, bindingType = BindingType.HumanMuscle }},
			//	Ring 1
			{ "RightHand.Ring.1 Stretched",		new ParsedCurveBinding() { boneName = "Right Ring Proximal", channelIndex = 2, bindingType = BindingType.HumanMuscle }},
			{ "RightHand.Ring.Spread",			new ParsedCurveBinding() { boneName = "Right Ring Proximal", channelIndex = 1, bindingType = BindingType.HumanMuscle }},
			//	Ring 2
			{ "RightHand.Ring.2 Stretched",		new ParsedCurveBinding() { boneName = "Right Ring Intermediate", channelIndex = 2, bindingType = BindingType.HumanMuscle }},
			//	Ring 3
			{ "RightHand.Ring.3 Stretched",		new ParsedCurveBinding() { boneName = "Right Ring Distal", channelIndex = 2, bindingType = BindingType.HumanMuscle }},
			//	Little 1
			{ "RightHand.Little.1 Stretched",	new ParsedCurveBinding() { boneName = "Right Little Proximal", channelIndex = 2, bindingType = BindingType.HumanMuscle }},
			{ "RightHand.Little.Spread",			new ParsedCurveBinding() { boneName = "Right Little Proximal", channelIndex = 1, bindingType = BindingType.HumanMuscle }},
			//	Little 2
			{ "RightHand.Little.2 Stretched",	new ParsedCurveBinding() { boneName = "Right Little Intermediate", channelIndex = 2, bindingType = BindingType.HumanMuscle }},
			//	Little 3
			{ "RightHand.Little.3 Stretched",	new ParsedCurveBinding() { boneName = "Right Little Distal", channelIndex = 2, bindingType = BindingType.HumanMuscle }},

			//	--- Left Leg ---
			//	LeftUpperLeg
			{ "Left Upper Leg Front-Back",		new ParsedCurveBinding() { boneName = "LeftUpperLeg", channelIndex = 2, bindingType = BindingType.HumanMuscle }},
			{ "Left Upper Leg In-Out",			new ParsedCurveBinding() { boneName = "LeftUpperLeg", channelIndex = 1, bindingType = BindingType.HumanMuscle }},
			{ "Left Upper Leg Twist In-Out",	new ParsedCurveBinding() { boneName = "LeftUpperLeg", channelIndex = 0, bindingType = BindingType.HumanMuscle }},
			//	LeftLowerLeg
			{ "Left Lower Leg Stretch",			new ParsedCurveBinding() { boneName = "LeftLowerLeg", channelIndex = 2, bindingType = BindingType.HumanMuscle }},
			{ "Left Lower Leg Twist In-Out",	new ParsedCurveBinding() { boneName = "LeftLowerLeg", channelIndex = 0, bindingType = BindingType.HumanMuscle }},
			//	LeftFoot
			{ "Left Foot Up-Down",				new ParsedCurveBinding() { boneName = "LeftFoot", channelIndex = 2, bindingType = BindingType.HumanMuscle }},
			{ "Left Foot Twist In-Out",			new ParsedCurveBinding() { boneName = "LeftFoot", channelIndex = 1, bindingType = BindingType.HumanMuscle }},
			//	LeftToes
			{ "Left Toes Up-Down",				new ParsedCurveBinding() { boneName = "LeftHand", channelIndex = 2, bindingType = BindingType.HumanMuscle }},

			//	--- Right Leg ---
			//	RightUpperLeg
			{ "Right Upper Leg Front-Back",		new ParsedCurveBinding() { boneName = "RightUpperLeg", channelIndex = 2, bindingType = BindingType.HumanMuscle }},
			{ "Right Upper Leg In-Out",			new ParsedCurveBinding() { boneName = "RightUpperLeg", channelIndex = 1, bindingType = BindingType.HumanMuscle }},
			{ "Right Upper Leg Twist In-Out",	new ParsedCurveBinding() { boneName = "RightUpperLeg", channelIndex = 0, bindingType = BindingType.HumanMuscle }},
			//	RightLowerLeg
			{ "Right Lower Leg Stretch",		new ParsedCurveBinding() { boneName = "RightLowerLeg", channelIndex = 2, bindingType = BindingType.HumanMuscle }},
			{ "Right Lower Leg Twist In-Out",	new ParsedCurveBinding() { boneName = "RightLowerLeg", channelIndex = 0, bindingType = BindingType.HumanMuscle }},
			//	RightFoot
			{ "Right Foot Up-Down",				new ParsedCurveBinding() { boneName = "RightFoot", channelIndex = 2, bindingType = BindingType.HumanMuscle }},
			{ "Right Foot Twist In-Out",		new ParsedCurveBinding() { boneName = "RightFoot", channelIndex = 1, bindingType = BindingType.HumanMuscle }},
			//	RightToes
			{ "Right Toes Up-Down",				new ParsedCurveBinding() { boneName = "RightHand", channelIndex = 2, bindingType = BindingType.HumanMuscle }},
		};
	}
}
}

#endif
