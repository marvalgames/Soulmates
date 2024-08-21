using Unity.Entities;

namespace Sandbox.Player
{
    [RequireMatchingQueriesForUpdate]
    public partial class PlayerRollSystem : SystemBase
    {


        protected override void OnUpdate()
        {

            Entities.WithoutBurst().ForEach(
                (
                    in InputControllerComponent inputController

                ) =>
                {

                    var buttonB = inputController.buttonB_Tap;


                }
            ).Run();

        }



    }
}


