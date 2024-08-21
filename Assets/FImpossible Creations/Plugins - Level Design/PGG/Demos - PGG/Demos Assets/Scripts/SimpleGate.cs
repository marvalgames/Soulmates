using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleGate : MonoBehaviour
{
    public bool Open = false;
    public List<AnimateHelp> ToMoveDown;
    public Vector3 Offset = Vector3.down;
    public float speed = 30f;

    private void Start()
    {
        for (int i = 0; i < ToMoveDown.Count; i++)
        {
            ToMoveDown[i].Init();
        }
    }

    public void OpenWithCamera()
    {
        StartCoroutine(CameraSwitch());
    }

    IEnumerator CameraSwitch()
    {
        yield return new WaitForSeconds(0.5f);

        Transform preFocus = SimpleIzoCamera.Instance.ToFollow;
        float preSpeed = SimpleIzoCamera.Instance.FollowDur;

        SimpleIzoCamera.Instance.ToFollow = transform;
        SimpleIzoCamera.Instance.FollowDur = 0.5f;
        
        yield return new WaitForSeconds(0.15f);
        Open = true;

        yield return new WaitForSeconds(4f);

        SimpleIzoCamera.Instance.ToFollow = preFocus;

        yield return new WaitForSeconds(2f);
         SimpleIzoCamera.Instance.FollowDur = preSpeed;
    }

    private void Update()
    {
        if (Open)
        {
            for (int i = 0; i < ToMoveDown.Count; i++)
                ToMoveDown[i].MoveToOffset(Offset, speed);
        }
        else
        {
            for (int i = 0; i < ToMoveDown.Count; i++)
                ToMoveDown[i].MoveToOffset(Vector3.zero, speed);
        }
    }


    [System.Serializable]
    public class AnimateHelp
    {
        public Transform transform;
        private Vector3 startPos;

        public void Init()
        {
            startPos = transform.position;
        }

        public void MoveToOffset(Vector3 offset, float speed)
        {
            Vector3 target = startPos + offset;
            transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
        }
    }
}
