using PixelCrushers.DialogueSystem;
using Rewired;
using UnityEngine;

namespace Quests
{
    public class QuestMenuGroup : MonoBehaviour
    {
        //[TextArea] public string startMessage = "Press Escape for Menu";
        private QuestLogWindow questLogWindow = null;
   
        void Start()
        {
            if (questLogWindow == null) questLogWindow = FindObjectOfType<QuestLogWindow>();
            //if (!string.IsNullOrEmpty(startMessage)) DialogueManager.ShowAlert(startMessage);
        }

   

        void Update()
        {
            var RightStickPressed = ReInput.players.GetPlayer(0).GetButtonDown("RightStickAction");
            if (RightStickPressed)
            {
                OpenQuestLog();
            }
        
    
        }

   
   

   
        private bool IsQuestLogOpen()
        {
            return (questLogWindow != null) && questLogWindow.isOpen;
        }

        private void OpenQuestLog()
        {
            if ((questLogWindow != null) && !IsQuestLogOpen())
            {
                questLogWindow.Open();
                Debug.Log("QUEST LOG " + questLogWindow);
            }
        }

   

  

    }
}