﻿using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Descriptions : MonoBehaviour
{
    private List<CanvasGroup> canvasList = new List<CanvasGroup>();

    void Start()
    {
        canvasList = GetComponentsInChildren<CanvasGroup>().ToList(); //linq using
    }

    public void ShowButtonCanvases(CanvasGroup showCanvasGroup)
    {
        foreach (var canvasGroup in canvasList)
        {
            canvasGroup.alpha = 0;
        }
        showCanvasGroup.alpha = 1;
    }



}
