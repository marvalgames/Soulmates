using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace JBooth.MicroVerseCore
{
    public static class IListExtensions
    {
        /// <summary>
        /// Shuffles the element order of the specified list.
        /// </summary>
        public static void Shuffle<T>(this IList<T> ts)
        {
            var count = ts.Count;
            var last = count - 1;
            for (var i = 0; i < last; ++i)
            {
                var r = UnityEngine.Random.Range(i, count);
                var tmp = ts[i];
                ts[i] = ts[r];
                ts[r] = tmp;
            }
        }
    }
    internal class ClipPlayer
    {
        public enum PlayOrder
        {
            Sequencial,
            Random
        }

        internal ClipPlayer(AudioClip[] c, AudioMixerGroup g, PlayOrder po)
        {
            clips = c;
            audioMixerGroup = g;
            playOrder = po;
        }

        List<AudioClip> clipList = new List<AudioClip>();

        AudioClip curClip;


        AudioClip[] clips;
        AudioMixerGroup audioMixerGroup;
        PlayOrder playOrder = PlayOrder.Random;

        AudioClip GetNextClip()
        {
            if (clips == null || clips.Length == 0)
            {
                Debug.LogError("Clip List is empty");
                return null;
            }
            if (clipList.Count == 0)
            {
                clipList.AddRange(clips);
                if (playOrder == PlayOrder.Random)
                {
                    clipList.Shuffle();
                }
            }
            if (playOrder == PlayOrder.Random && clipList.Count == 1 && clips.Length > 1)
            {
                // random, no repeats
                var clip = clipList[0];
                clipList.Clear();
                clipList.AddRange(clips);
                clipList.Remove(clip);
                return clip;
            }
            AudioClip c = clipList[0];
            clipList.RemoveAt(0);
            return c;

        }

        float oldVolume = 0;
        AudioSource src;
        internal void UpdatePlayer(float audioLevel)
        {
            if (audioLevel <= 0 && oldVolume <= 0)
                return;
            audioLevel *= audioLevel;
            if (clips.Length > 0)
            {
                if (curClip == null)
                {
                    curClip = GetNextClip();
                    src = AmbianceMgr.GetAudioSource(curClip, clips.Length > 1);
                    src.clip = curClip;
                    src.outputAudioMixerGroup = audioMixerGroup;
                    if (clips.Length == 1)
                        src.loop = true;
                }
                if (oldVolume != audioLevel)
                {
                    if (oldVolume <= 0 && audioLevel > 0)
                    {
                        src.Play();
                    }
                    else if (oldVolume > 0 && audioLevel <= 0)
                    {
                        src.Pause();
                    }
                    oldVolume = audioLevel;
                    src.volume = audioLevel;
                }

                if (clips.Length > 1)
                {
                    // TODO: this seems to produce a gap even though it should loop perfectly.
                    double remainder = (double)(src.clip.samples - src.timeSamples)
                          / (double)src.clip.frequency;

                    if (remainder < 1)
                    {
                        curClip = GetNextClip();
                        src = AmbianceMgr.GetAudioSource(curClip, true);
                        src.outputAudioMixerGroup = audioMixerGroup;
                        src.PlayScheduled(AudioSettings.dspTime + remainder);
                    }
                }
            }
        }
    }
}