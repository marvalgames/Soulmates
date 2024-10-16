﻿using PixelCrushers.DialogueSystem;
using Sandbox.Player;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Events;

namespace Quests
{
    public class QuestEnemiesVariableCheck : MonoBehaviour
    {
    
        public PlayerQuests playerQuesting;

        [Tooltip("Increment this Dialogue System variable.")] [VariablePopup]
        public string variable = string.Empty;

        [Tooltip("Optional alert message to show when incrementing.")]
        public string alertMessage = string.Empty;

        [Tooltip("Duration to show alert, or 0 to use default duration.")]
        public float alertDuration = 0;

        int previousCount;
        private Entity questerEntity;
        private EntityManager manager;

        public UnityEvent onQuestVariableUpdate = new UnityEvent();

        protected virtual string actualVariableName
        {
            get { return string.IsNullOrEmpty(variable) ? DialogueActor.GetPersistentDataName(transform) : variable; }
        }

        void Start()
        {
         

            if (manager == default)
            {
                manager = World.DefaultGameObjectInjectionWorld.EntityManager;
            
            }

        }
        // Update is called once per frame
        void Update()
        {
            
            if(manager == default) return;
            if (QuestLog.GetQuestState("Enemy Attack") == QuestState.Unassigned)
            {
                return;
            }


            if (playerQuesting && questerEntity == Entity.Null)
            {
                questerEntity = playerQuesting.e;
      
            }
            
            var hasMissionComponent = manager.HasComponent<MissionComponent>(questerEntity);
            if(!hasMissionComponent) return;
            var missionComponent = manager.GetComponentData<MissionComponent>(questerEntity);
            Debug.Log("quest destroyed " + missionComponent.questUpdateEnemiesDestroyed);
            if (!missionComponent.questUpdateEnemiesDestroyed) return;

            

            var dead = missionComponent.questEnemiesDestroyed;
            missionComponent.questUpdateEnemiesDestroyed = false;
            manager.SetComponentData(questerEntity, missionComponent);
            DialogueLua.SetVariable(actualVariableName, dead);
            Debug.Log("quest enemies dead " + dead);
            DialogueManager.SendUpdateTracker();
            if (!(string.IsNullOrEmpty(alertMessage) || !DialogueManager.instance))
            {
                if (Mathf.Approximately(0, alertDuration))
                {
                    DialogueManager.ShowAlert(alertMessage);
                }
                else
                {
                    DialogueManager.ShowAlert(alertMessage, alertDuration);
                }
            }

            onQuestVariableUpdate.Invoke();
        }
    }
}