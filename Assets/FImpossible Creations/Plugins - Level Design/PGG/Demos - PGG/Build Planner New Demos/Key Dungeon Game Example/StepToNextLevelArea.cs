using UnityEngine;

namespace FIMSpace.Generating
{
    public class StepToNextLevelArea : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            if ( other.tag == "Player")
            {
                if (DungeonGameController_PGGDemo.Instance) DungeonGameController_PGGDemo.Instance.StepToNextLevel();
                else
                SimpleGameController.Instance.StepToNextLevel();
                GameObject.Destroy(gameObject);
            }
        }
    }
}