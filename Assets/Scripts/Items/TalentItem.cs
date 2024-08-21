using Sandbox.Player;
using Unity.Entities;
using UnityEngine;





public class TalentItem : MonoBehaviour
{
    [SerializeField]
    private TalentScriptableObject talentScriptableObject;


    [Tooltip("If active then pickup not required")]
    public GameObject actorTalentOwner;
    public bool active = true;
    public bool immediateUse;
   
  
    public AudioClip powerEnabledAudioClip;
    public AudioClip powerTriggerAudioClip;
    public AudioSource audioSource;


    class TalentBaker : Baker<TalentItem>
    {
        public override void Bake(TalentItem authoring)
        {
            var ps = authoring.GetComponentInChildren<ParticleSystem>();
            var psEntity = GetEntity(ps, TransformUsageFlags.Dynamic);
            var entity = GetEntity(TransformUsageFlags.Dynamic);
          
            AddComponent(entity, new TalentItemComponent
            {
                
                pickedUpActor = GetEntity(authoring.actorTalentOwner, TransformUsageFlags.Dynamic),
                particleSystemEntity = psEntity,
                active = authoring.active,
                description = authoring.talentScriptableObject.talentItemDescription,
                longDescription = authoring.talentScriptableObject.talentItemLongDescription,
                pickupEntity = entity,
                index = entity.Index,
                talentType = authoring.talentScriptableObject.talentType,
                itemPickedUp = authoring.active,
                statDescription1 = authoring.talentScriptableObject.statDescription1,
                statDescription2 = authoring.talentScriptableObject.statDescription2,
                statDescription3 = authoring.talentScriptableObject.statDescription3,
                statRating1 = authoring.talentScriptableObject.statRating1,
                statRating2 = authoring.talentScriptableObject.statRating2,
                statRating3 = authoring.talentScriptableObject.statRating3,
                statDescriptionLong1 = authoring.talentScriptableObject.statLongDescription1,
                statDescriptionLong2 = authoring.talentScriptableObject.statLongDescription2,
                statDescriptionLong3 = authoring.talentScriptableObject.statLongDescription3
                    
            });

        }
    }












}

