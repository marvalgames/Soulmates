using UnityEngine;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.Hybrid
{
[RequireComponent(typeof(RigDefinitionAuthoring))]
public class AnimationAssetSetAuthoring: MonoBehaviour
{
	public AnimationClip[] animationClips;
	public AvatarMask[] avatarMasks;
}
}
