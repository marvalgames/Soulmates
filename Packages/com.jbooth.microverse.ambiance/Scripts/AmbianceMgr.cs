using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JBooth.MicroVerseCore
{
    [ExecuteInEditMode]
    public class AmbianceMgr : MonoBehaviour
    {
        public Transform listener;

        static AmbianceMgr _instance;

        private void Awake()
        {
            if (_instance != null)
            {
                DestroyImmediate(_instance);
            }
            _instance = this;
            GameObject.DontDestroyOnLoad(this);
        }

        public static bool Exists() { return _instance != null; }

        public static void EnsureExists()
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("MusicManager");
                _instance = go.AddComponent<AmbianceMgr>();
            }
        }

        public static AmbianceMgr Instance
        {
            get
            {
                EnsureExists();
                return _instance;
            }
        }

        static List<AmbientArea> ambientAreas = new List<AmbientArea>();
        internal static void RegisterArea(AmbientArea a)
        {
            ambientAreas.Add(a);
        }

        internal static void UnregisterArea(AmbientArea a)
        {
            ambientAreas.Remove(a);
        }

        static float _intensityLevel;
        public static float intensityLevel
        {
            get { return _intensityLevel; }
            set { _intensityLevel = Mathf.Clamp01(value); }
        }

        static float _musicLevel = 1;
        public static float musicLevel
        {
            get { return _musicLevel; }
            set { _musicLevel = Mathf.Clamp01(value); }
        }

        static float _ambientLevel = 1;
        public static float ambientLevel
        {
            get { return _ambientLevel; }
            set { _ambientLevel = Mathf.Clamp01(value); }
        }

        static Stack<AudioSource> audioSourcePool = new Stack<AudioSource>();
        static List<AudioSource> releaseSourceList = new List<AudioSource>();

        static GameObject sourceHolder = null;
        internal static AudioSource GetAudioSource(AudioClip c, bool autorelease)
        {
            if (audioSourcePool.Count == 0)
            {
                if (sourceHolder == null)
                {
                    sourceHolder = new GameObject("Sound");
                    sourceHolder.hideFlags = HideFlags.HideAndDontSave;
                }
                var s = sourceHolder.AddComponent<AudioSource>();
                s.loop = false;
                s.playOnAwake = false;
                s.bypassReverbZones = true;
                s.spatialize = false;
                s.spatialBlend = 0;
                if (autorelease)
                {
                    releaseSourceList.Add(s);
                }
                s.clip = c;
                return s;
            }
            else
            {
                var s = audioSourcePool.Pop();
                s.clip = c;
                if (autorelease)
                {
                    releaseSourceList.Add(s);
                }
                return s;
            }
        }

        internal static void ReleaseAudioSource(AudioSource s)
        {
            if (s != null)
            {
                releaseSourceList.Add(s);
            }
        }

        private void Update()
        {
            // release any sources that are no longer playing
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

            Vector3 position = Vector3.zero;
            if (listener != null)
            {
                position = listener.position;
            }
            else
            {
                Camera c = Camera.main;
                if (c != null)
                {
                    listener = c.transform;
                    position = c.transform.position;
                }
            }

            // ambient
            for (int i = 0; i < ambientAreas.Count; ++i)
            {
                var aa = ambientAreas[i];
                aa.UpdateArea(position);
            }


        }
    }
}