using Unity.Entities;
using UnityEngine;



public enum SlashStates
{
    None,
    Started,
    InAction,
    Ended
    
}

public struct SlashComponent : IComponentData
{
    public bool slashActive;
    public int slashState;
    public float hkDamage;//for ld
    public bool animate;


}

public class SlashClass : IComponentData
{
    public AudioSource audioSource;

}


public class SlashComponentAuthoring : MonoBehaviour

{
    public bool slashActive;
    public AudioSource audioSource;
    public AudioClip audioClip;


    class SlashBaker : Baker<SlashComponentAuthoring>
    {
        public override void Bake(SlashComponentAuthoring authoring)
        {
            var e = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);

            AddComponent(e, new SlashComponent()
                {
                    slashActive = authoring.slashActive,
                    slashState = (int) SlashStates.None,

                }

            );


            if (authoring.audioSource != null)
            {
                //Debug.Log("BAKING SLASH CLASS AUDIO SOURCE");
                var _audioSource = authoring.audioSource;
                _audioSource.clip = authoring.audioClip;

                AddComponentObject(e, new SlashClass()
                    {
                        audioSource = _audioSource

                    }

                );
            }



        }
    }

}
