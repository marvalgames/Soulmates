using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JBooth.MicroVerseCore
{
    internal class AmbientState
    {

        static Stack<AudioSource> audioSourcePool = new Stack<AudioSource>();
        static List<AudioSource> releaseSourceList = new List<AudioSource>();


        List<RandomSoundState> soundStates = new List<RandomSoundState>();

        internal Ambient ambient;

        internal static AudioSource GetAudioSource()
        {
            if (audioSourcePool.Count == 0)
            {
                GameObject go = new GameObject("Ambient Sound");
                go.hideFlags = HideFlags.HideAndDontSave;
                var s = go.AddComponent<AudioSource>();
                s.loop = false;
                s.hideFlags = HideFlags.DontSave;
                s.playOnAwake = false;
                releaseSourceList.Add(s);
                return s;
            }
            else
            {
                var s = audioSourcePool.Pop();
                releaseSourceList.Add(s);
                return s;
            }
        }

        internal class RandomSoundState
        {
            internal Ambient.RandomSound rs;

            float timeUntilTrigger = 0;

            void NextTrigger()
            {
                timeUntilTrigger = UnityEngine.Random.Range(rs.delay - rs.delayVariance, rs.delay + rs.delayVariance);
                if (timeUntilTrigger < 0.05f)
                    timeUntilTrigger = Mathf.Max(rs.delay, 0.05f);
            }

            internal RandomSoundState(Ambient.RandomSound r)
            {
                rs = r;
                NextTrigger();
            }

            internal void UpdateState(Ambient a, AmbientArea area, Vector3 listenerPos)
            {
                timeUntilTrigger -= Time.smoothDeltaTime * area.audioChance;
                if (timeUntilTrigger < 0)
                {
                    var s = GetAudioSource();
                    s.pitch = Random.Range(rs.pitch - rs.pitchVariance, rs.pitch + rs.pitchVariance);
                    s.volume = Random.Range(rs.volume - rs.volumeVariance, rs.volume + rs.volumeVariance) * AmbianceMgr.ambientLevel;
                    s.spatialBlend = rs.spacialization;
                    s.outputAudioMixerGroup = a.outputGroup;
                    s.spatialize = rs.spacialization > 0 ? true : false;

                    if (s.spatialize)
                    {
                        Vector3 soundPos;
                        if (area.falloff == AmbientArea.AmbianceFalloff.Range || rs.playerRadius > 0)
                        {
                            soundPos = Random.insideUnitSphere;
                        }
                        else
                        {
                            soundPos = new Vector3(Random.Range(-1.0f, 1.0f),
                               Random.Range(-1.0f, 1.0f),
                               Random.Range(-1.0f, 1.0f));
                        }

                        if (rs.playerRadius > 0)
                        {
                            soundPos = listenerPos + soundPos * rs.playerRadius;
                        }
                        else
                        {
                            soundPos = area.transform.localToWorldMatrix.MultiplyPoint(soundPos);
                        }

                        if (rs.minimumDistance > 0)
                        {
                            float dist = Vector3.Distance(soundPos, listenerPos);
                            if (dist < rs.minimumDistance)
                            {
                                soundPos = Vector3.Normalize(soundPos - listenerPos) * rs.minimumDistance;
                            }
                        }
                        s.transform.position = soundPos;
                    }
                    var clip = rs.clips[UnityEngine.Random.Range(0, rs.clips.Length)];
                    if (clip != null)
                    {
                        s.PlayOneShot(clip);
                    }
                    else
                    {
                        Debug.LogWarning("Null Clip found in Ambient " + a.name);
                    }


                    NextTrigger();
                }
            }
        }

        internal AmbientState(Ambient a)
        {
            ambient = a;
            for (int i = 0; i < a.randomSounds.Length; ++i)
            {
                var rs = a.randomSounds[i];
                if (rs.clips == null || rs.clips.Length == 0)
                {
                    Debug.LogWarning("Ambient with no clips in Random Sound list");
                }
                else
                {
                    RandomSoundState state = new RandomSoundState(rs);
                    soundStates.Add(state);
                }
            }
        }


        internal void UpdateState(AmbientArea area, Vector3 listenerPos)
        {
            for (int i = 0; i < releaseSourceList.Count; ++i)
            {
                if (!releaseSourceList[i].isPlaying)
                {
                    var s = releaseSourceList[i];
                    releaseSourceList.RemoveAt(i);
                    i--;
                    audioSourcePool.Push(s);

                }
            }
            for (int i = 0; i < soundStates.Count; ++i)
            {
                soundStates[i].UpdateState(ambient, area, listenerPos);
            }
        }

    }
}
