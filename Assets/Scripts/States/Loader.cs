﻿using UnityEngine;
using UnityEngine.SceneManagement;

namespace States
{
    public class Loader : MonoBehaviour
    {
        public float loadTime = 2.0f;
        private int currentSceneIndex;


        void Awake()
        {
            var allListeners = FindObjectsOfType<AudioListener>();
            if (allListeners.Length > 1)
            {
                for (var i = 1; i < allListeners.Length; i++)
                {
                    DestroyImmediate(allListeners[i]);
                }
            }

            var allCameras = FindObjectsOfType<Camera>();
            if (allCameras.Length > 1)
            {
                foreach (var cam in allCameras)
                {
                    if (!cam.gameObject.CompareTag("MainCamera"))
                    {
                        DestroyImmediate(cam.gameObject);
                    }
                }
            }
        }


        void Start()
        {



            currentSceneIndex = SceneManager.GetActiveScene().buildIndex;


            if (SceneManager.GetActiveScene().buildIndex <= 0)
            {
                LoadYourScene(1);
            }
            else
            {
                LoadYourScene(currentSceneIndex);
            }
        }

        void LoadYourScene(int sceneIndex)
        {
            SceneManager.LoadScene(sceneIndex);
            Debug.Log(sceneIndex + " loaded");
        }
    }
}