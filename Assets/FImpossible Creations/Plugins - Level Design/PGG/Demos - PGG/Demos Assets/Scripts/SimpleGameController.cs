using System.Collections;
using UnityEngine;

namespace FIMSpace.Generating
{
    public class SimpleGameController : MonoBehaviour
    {
        public static SimpleGameController Instance { get; private set; }

        public PGGPlanGeneratorBase GeneratedsSource;
        public GameObject PlayerObject;

        [Header("Gameplay Elements (Auto getted on start)")]
        public SimpleGate BossKeyGate;
        public SimpleGate FinishLevelGate;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            StartCoroutine(NextLevel(GeneratedsSource.Generated.Count == 0));
        }

        public void StepToNextLevel()
        {
            if (!generatingLevel) StartCoroutine(NextLevel(true));
        }

        bool generatingLevel = false;
        private IEnumerator NextLevel(bool regenerate )
        {
            PlayerObject.GetComponent<Rigidbody>().useGravity = false;

            generatingLevel = true;
            yield return new WaitForSeconds(0.25f);

            if (regenerate)
            {
                GeneratedsSource.ClearGenerated();

                GeneratedsSource.transform.position = PlayerObject.transform.position - new Vector3(2, 0, 2);
                GeneratedsSource.transform.position = new Vector3(GeneratedsSource.transform.position.x, 0, GeneratedsSource.transform.position.z);

                GeneratedsSource.PlanGuides[0].Start = Vector2Int.zero;
                GeneratedsSource.PlanGuides[0].End = GeneratedsSource.PlanGuides[0].Start + new Vector2Int(FGenerators.GetRandom(28, 30), FGenerators.GetRandom(-16, 16));
                GeneratedsSource.Seed += 1;

                GeneratedsSource.SizeLimitX.Min = GeneratedsSource.PlanGuides[0].Start.x + 2;
                GeneratedsSource.SizeLimitX.Max = GeneratedsSource.PlanGuides[0].End.x - 2;

                GeneratedsSource.GenerateObjects();

                yield return new WaitForSeconds(1f);
            }

            OnLevelStartReady();
            PlayerObject.GetComponent<Rigidbody>().useGravity = true;

            yield return new WaitForSeconds(1f);
            generatingLevel = false;
        }

        public void OnLevelStartReady()
        {
            FieldSetup bossField = GeneratedsSource.BuildPlanPreset.GetFieldSetupOfRoom("Boss Room");
            BossKeyGate = GeneratedsSource.GetComponentFromField<SimpleGate>(bossField);

            FieldSetup corridorField = GeneratedsSource.BuildPlanPreset.RootChunkSetup.FieldSetup;
            FinishLevelGate = GeneratedsSource.GetComponentFromField<SimpleGate>(corridorField);
        }

        public void OnKeyCollected()
        {
            BossKeyGate.OpenWithCamera();
        }

        public void OnBossDeath()
        {
            FinishLevelGate.OpenWithCamera();
        }

        void Update()
        {

        }
    }
}