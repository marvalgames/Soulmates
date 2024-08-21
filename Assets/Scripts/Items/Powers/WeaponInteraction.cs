// using RootMotion.FinalIK;
// using Unity.Entities;
// using UnityEngine;
//
// public struct WeaponInteractionComponent : IComponentData
// {
//     public int weaponType;
//     public bool canPickup;
// }
//
// public struct CharacterInteractionComponent : IComponentData
// {
//
// }
//
//
//
// public class WeaponInteraction : MonoBehaviour
// {
//     public Entity e;
//     private EntityManager manager;
//
//     [SerializeField] private InteractionSystem interactionSystem;
//     //    [HideInInspector]
//     public InteractionObject interactionObject;//set from  picked up weaponitem
//
//     public bool interactKeyPressed;
//     [SerializeField]
//     private bool inputRequired;
//
//
//     public bool canPickup;
//
//     private AudioSource audioSource;
//     [SerializeField]
//     private AudioClip clip;
//
//     void Start()
//     {
//
//         var manager = World.DefaultGameObjectInjectionWorld.EntityManager;
//         var entity = manager.CreateEntity();
//         manager.AddComponentData(entity,
//
//          new WeaponInteractionComponent { weaponType = 0, canPickup = canPickup }
//
//         );
//         manager.AddComponentObject(entity, this);
//
//         interactionSystem.OnInteractionStop += OnStop;
//         audioSource = GetComponent<AudioSource>();
//
//
//     }
//
//     private void OnStop(FullBodyBipedEffector effectorType, InteractionObject interactionObject)
//     {
//
//         //var weaponItem = manager.GetComponentData<WeaponItemComponent>(e);
//         //weaponItem.pickedUp = true;
//         //manager.SetComponentData(e, weaponItem);
//
//         interactionSystem.ik.enabled = false;
//         manager.RemoveComponent<CharacterInteractionComponent>(e);
//
//     }
//
//     private void LateUpdate()
//     {
//         interactionSystem.ik.solver.Update();
//     }
//
//
//     public void UpdateSystem()
//     {
//         if ((interactKeyPressed || inputRequired == false) && interactionSystem.inInteraction == false && interactionObject != null)
//         {
//             Debug.Log("interaction ");
//
//             interactionSystem.StartInteraction(FullBodyBipedEffector.RightHand, interactionObject, true);
//             interactionSystem.ik.enabled = true;
//
//         }
//
//     }
//
//
//
//
//
//
//
//
//     void OnDestroy()
//     {
//         if (interactionSystem == null) return;
//         interactionSystem.OnInteractionStop -= OnStop;
//     }
// }
//
//
// public partial class PowerInteractionSystem : SystemBase
// {
//
//     protected override void OnUpdate()
//     {
//         Entities.WithoutBurst().WithStructuralChanges().ForEach
//         (
//             (
//                 WeaponInteraction weaponInteraction,
//                 in InputControllerComponent inputController
//
//             ) =>
//             {
//                 weaponInteraction.interactKeyPressed = inputController.buttonB_Tap;
//             }
//
//         ).Run();
//
//
//
//     }
// }
