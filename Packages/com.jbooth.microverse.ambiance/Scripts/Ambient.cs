using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace JBooth.MicroVerseCore
{
    [CreateAssetMenu(fileName = "ambient", menuName = "MicroVerse/Ambient")]
    public class Ambient : ScriptableObject
    {
        [System.Serializable]
        public class RandomSound
        {
            [Tooltip("List of audio clips to randomly choose from")]
            public AudioClip[] clips;
            [Tooltip("When non 0, the sound is centered around the player instead of played in the area, and is created within this radius.")]
            public float playerRadius;
            [Tooltip("Pushes the sound away from the player so it never gets too close")]
            public float minimumDistance;

            [Tooltip("Average delay between playing one of these sounds")]
            public float delay;
            [Tooltip("How much to vary the delay by")]
            public float delayVariance;
            [Tooltip("Volume to play clips at")]
            public float volume;
            [Tooltip("How much to vary the volume by")]
            public float volumeVariance;
            [Tooltip("Pitch to play the sound at, 1 is default")]
            public float pitch;
            [Tooltip("How much to vary the pitch by each time the sound is played")]
            public float pitchVariance;

            [Range(0, 1)]
            [Tooltip("0 is 2d, 1 is fully 3d")]
            public float spacialization;
        }

        [Tooltip("Optional output mixer group to route audio to")]
        public AudioMixerGroup outputGroup;
        [Tooltip("List of sounds to play randomly around the area or player")]
        public RandomSound[] randomSounds;
        [Tooltip("Backgroud tracks to be looped at random")]
        public AudioClip[] backgroundLoops;
        [Tooltip("Volume for background loops")]
        [Range(0, 1)]
        public float backgroundVolume = 1;
    }
}