using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FIMSpace.Generating
{
    public class ExampleSpawnAnimation : MonoBehaviour
    {
        public PGGGeneratorBase generator;
        public float animDur = 1f;
        public float perOneDelay = 0.02f;

        public enum EStyle { FromDown, FromUp, RandomDir }
        public EStyle AnimationStyle = EStyle.FromDown;
        public enum EMode { Move, Rotate, Scale }
        public EMode Mode = EMode.Move;
        public enum EEase { Elastic, Bounce, Smooth }
        public EEase AnimationEase = EEase.Elastic;

        public bool ReverseOrder = false;
        public bool WaitFramesBeforeGenerating = false;

        public float distanceTo = 5f;

        private void Start()
        {
            if (generator == null)
                generator = GetComponent<PGGGeneratorBase>();
        }

        public class AnimHelper
        {
            public Transform transform;
            public Vector3 initPos;
            public Quaternion initRot;
            public Vector3 initScale;
            public EMode mode;
            public AnimHelper(Transform t, float dist, EStyle anim, EMode mode)
            {
                transform = t;
                initPos = t.position;
                initRot = t.rotation;
                initScale = t.localScale;
                this.mode = mode;

                if (mode == EMode.Move)
                {
                    if (anim == EStyle.FromDown)
                        t.position += Vector3.down * dist;
                    else if (anim == EStyle.FromUp)
                        t.position += Vector3.up * dist;
                    else
                    {
                        Quaternion randDir = Quaternion.Euler(Random.Range(-180f, 180f), Random.Range(-180, 180), 0);
                        t.position += randDir * (Vector3.forward * dist);
                    }
                }
                else if (mode == EMode.Rotate)
                {
                    if (anim == EStyle.FromDown)
                        t.rotation *= Quaternion.Euler(90, 0, 0);
                    else if (anim == EStyle.FromUp)
                        t.rotation *= Quaternion.Euler(-179, 0, 0);
                    else
                        t.rotation *= Quaternion.Euler(Random.Range(0f, 120f) + 90, Random.Range(-35, 35), Random.Range(-35, 35));
                }
                else if (mode == EMode.Scale)
                {
                    if (anim == EStyle.FromDown)
                        t.localScale = new Vector3(1f, 0f, 1f);
                    else if (anim == EStyle.FromUp)
                        t.localScale = new Vector3(0f, 1f, 0f);
                    else
                        t.localScale = new Vector3(Random.Range(0f, 0.5f), Random.Range(0f, 0.5f), 0f);
                }

                t.gameObject.SetActive(false);
            }
        }

        IEnumerator StartDelayed()
        {
            if (WaitFramesBeforeGenerating)
            {
                yield return null;
                yield return null;
            }

            List<GameObject> allGenerated = generator.GetAllGeneratedObjects(false);

            for (int i = allGenerated.Count - 1; i >= 0; i--) // Make sure there are no containers
                if (allGenerated[i].name.Contains("Contain"))
                    allGenerated.RemoveAt(i);

            if (ReverseOrder) allGenerated.Reverse();
            for (int i = 0; i < allGenerated.Count; i++)
            {
                StartCoroutine(SpawnAnimation(new AnimHelper(allGenerated[i].transform, distanceTo, AnimationStyle, Mode), (float)i * perOneDelay, animDur, AnimationEase));
            }

            yield break;
        }

        public void RunSpawnAnimations()
        {
            StartCoroutine(StartDelayed());
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.G))
            {
                StopAllCoroutines();
                generator.Seed += 1;
                generator.GenerateObjects();
            }
        }

        public static IEnumerator SpawnAnimation(AnimHelper helper, float delay, float animDur, EEase animationEase)
        {
            yield return null;
            yield return new WaitForSeconds(delay);

            float elapsed = 0f;
            if (helper.transform == null) yield break;
            Vector3 start = helper.transform.position;
            Quaternion startR = helper.transform.rotation;
            Vector3 startS = helper.transform.localScale;
            helper.transform.gameObject.SetActive(true);

            while (elapsed < animDur)
            {
                if (helper.transform == null) yield break;
                float progr = elapsed / animDur;

                if (helper.mode == EMode.Move)
                    helper.transform.position = Vector3.LerpUnclamped(start, helper.initPos, Ease(animationEase, progr));
                else if (helper.mode == EMode.Rotate)
                    helper.transform.rotation = Quaternion.LerpUnclamped(startR, helper.initRot, Ease(animationEase, progr));
                else if (helper.mode == EMode.Scale)
                    helper.transform.localScale = Vector3.LerpUnclamped(startS, helper.initScale, Ease(animationEase, progr));

                elapsed += Time.deltaTime;
                yield return null;
            }

            if (helper.transform == null) yield break;
            helper.transform.position = helper.initPos;
        }

        public static float Ease(EEase style, float v)
        {
            switch (style)
            {
                case EEase.Elastic: return Elastic(v);
                case EEase.Bounce: return Bounce(v);
                case EEase.Smooth: return Smooth(v);
            }

            return Elastic(v);
        }

        public static float Elastic(float k)
        {
            if (k == 0) return 0; if (k == 1) return 1;
            return Mathf.Pow(2f, -10f * k) * Mathf.Sin((k - 0.1f) * (2f * Mathf.PI) / 0.4f) + 1f;
        }

        public static float Smooth(float k)
        {
            return Mathf.Sin(k * Mathf.PI / 2f);
        }

        public static float Bounce(float k)
        {
            if (k < (1f / 2.75f))
            {
                return 7.5625f * k * k;
            }
            else if (k < (2f / 2.75f))
            {
                return 7.5625f * (k -= (1.5f / 2.75f)) * k + 0.75f;
            }
            else if (k < (2.5f / 2.75f))
            {
                return 7.5625f * (k -= (2.25f / 2.75f)) * k + 0.9375f;
            }
            else
            {
                return 7.5625f * (k -= (2.625f / 2.75f)) * k + 0.984375f;
            }
        }
    }
}
