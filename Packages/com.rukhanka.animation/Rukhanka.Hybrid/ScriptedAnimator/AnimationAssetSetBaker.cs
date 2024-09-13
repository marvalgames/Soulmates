#if UNITY_EDITOR

using Unity.Entities;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.Hybrid
{
public class AnimationAssetSetBaker: Baker<AnimationAssetSetAuthoring>
{
	public override void Bake(AnimationAssetSetAuthoring a)
	{
		var rigDef = GetComponent<RigDefinitionAuthoring>();
		var avatar = rigDef.GetAvatar();
		
		var animationBaker = new AnimationClipBaker();
		var bakedAnimations = animationBaker.BakeAnimations(this, a.animationClips, avatar, a.gameObject);
		var e = GetEntity(a, TransformUsageFlags.None);
		var newAnimArr = AddBuffer<NewBlobAssetDatabaseRecord<AnimationClipBlob>>(e);
		foreach (var ba in bakedAnimations)
		{
			var newAnim = new NewBlobAssetDatabaseRecord<AnimationClipBlob>()
			{
				hash = ba.Value.hash,
				value = ba
			};
			
			newAnimArr.Add(newAnim);
		}
	}
}
}
  
#endif
