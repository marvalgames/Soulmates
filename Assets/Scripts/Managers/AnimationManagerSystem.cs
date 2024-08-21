using Enemy;
using Unity.Entities;
using UnityEngine;

namespace Managers
{
    [UpdateAfter(typeof(EnemyDefenseMoves))]
    [RequireMatchingQueriesForUpdate]
    public partial class AnimationManagerSystem : SystemBase
    {
        private static readonly int EvadeStrike = Animator.StringToHash("EvadeStrike");

        protected override void OnUpdate()
        {
            Entities.WithoutBurst().ForEach((Animator animator,ref EvadeComponent evadeComponent,
                    ref AnimationManagerComponentData animationManagerData) =>
                {
                    var evadeStrike = animationManagerData.evadeStrike;
                    animator.SetBool(EvadeStrike, evadeStrike);
                    evadeComponent.evadeStrike = evadeStrike;
                }
            ).Run();
    
    
        }
    }
}