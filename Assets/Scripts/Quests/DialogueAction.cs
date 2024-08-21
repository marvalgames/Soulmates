using PixelCrushers.DialogueSystem;
using UnityEngine;

namespace Quests
{
    public class DialogueAction : MonoBehaviour
    {
        Animator animator;
        // Use this for initialization
        void Start()
        {
            animator = GetComponent<Animator>();
            //DialogueManager.ShowAlert("HEY YOU");
        }

        public void OnBarkStart()
        {
            Debug.Log("bark start");
            //animator.SetInteger("DialogState", 1);
        }

        public void OnBarkEnd()
        {
            Debug.Log("bark end");
            //animator.SetInteger("DialogState", 0);
        }

        public void OnConversationStart()
        {
            Debug.Log("conversation start");
            //animator.SetInteger("DialogState", 1);
            GameInterface.instance.EnableControllerMaps(false, true, false);
            GameInterface.instance.StateChange = true;
            GameInterface.instance.Paused = true;
        }

        public void OnConversationEnd()
        {
            Debug.Log("conversation end");
            //animator.SetInteger("DialogState", 0);
            GameInterface.instance.EnableControllerMaps(true, false, false);
            GameInterface.instance.StateChange = true;
            GameInterface.instance.Paused = false;

        }
        public void OnUsableStart()
        {
            //Debug.Log("usable start");
            GameInterface.instance.EnableControllerMaps(false, false, true);
            //animator.SetInteger("DialogState", 1);
        
       
        }

        public void OnUsableEnd()
        {
            //Debug.Log("usable end");
            if (GameInterface.instance.ActiveGameControllerMap == GameControllerMap.DialogMap)
            {
                GameInterface.instance.EnableControllerMaps(true, false, false);
            }
            //animator.SetInteger("DialogState", 0);
        }

        public void QuestStateChange(string questName)
        {

            bool success = QuestLog.IsQuestSuccessful(questName);
            Debug.Log("success " + success);
            if (success)
            {
                DialogueManager.ShowAlert("Success");
                DialogueManager.ShowAlert("Success");

            }

        }



        // Update is called once per frame
        void Update()
        {
            //Debug.Log("conversation update");
        }

    }
}