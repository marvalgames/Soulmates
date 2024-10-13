using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.VFX;

/////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.Samples
{
	[UpdateAfter(typeof(RukhankaAnimationSystemGroup))]
	[WorldSystemFilter(WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.ClientSimulation)]
    public partial class SampleEventReactionSystem : SystemBase
    {
	    EventsSampleConf cfg;
	    
	    protected override void OnStartRunning()
	    {
		    cfg = GameObject.FindFirstObjectByType<EventsSampleConf>();
	    }

	    protected override void OnUpdate()
	    {
		    if (cfg == null)
			    return;
		    
		    float3 leftLegPos = 0, rightLegPos = 0;
		    
		    foreach (var (_, lt) in SystemAPI.Query<EllenLeftFootTag, LocalTransform>())
		    {
			    leftLegPos = lt.Position;
		    }
		    
		    foreach (var (_, lt) in SystemAPI.Query<EllenRightFootTag, LocalTransform>())
		    {
			    rightLegPos = lt.Position;
		    }
		    
		    foreach (var aes in SystemAPI.Query<DynamicBuffer<AnimationEventComponent>>())
		    {
			    foreach (var ae in aes)
			    {
				    if (ae.intParam == 0)
					    GameObject.Instantiate(cfg.walkStepLParticle, new Vector3(leftLegPos.x, leftLegPos.y, leftLegPos.z), Quaternion.Euler(0, 0, 45));

				    if (ae.intParam == 1)
					    GameObject.Instantiate(cfg.walkStepRParticle, new Vector3(rightLegPos.x, rightLegPos.y, rightLegPos.z), Quaternion.Euler(0, 0, -45));
			    }
		    }
		    
		    foreach (var aces in SystemAPI.Query<DynamicBuffer<AnimatorControllerEventComponent>>())
		    {
			    foreach (var ace in aces)
			    {
				    if (ace.eventType == AnimatorControllerEventComponent.EventType.StateEnter)
				    {
					    var color = new Vector4(1, 1, 0, 1);
					    if (ace.stateId == 1)
						    color = new Vector4(1, 0, 0, 1);
					    cfg.walkStepRParticle.GetComponent<VisualEffect>().SetVector4("myColor", color);
					    cfg.walkStepLParticle.GetComponent<VisualEffect>().SetVector4("myColor", color);
				    }
			    }
		    }
	    }
    }
}
