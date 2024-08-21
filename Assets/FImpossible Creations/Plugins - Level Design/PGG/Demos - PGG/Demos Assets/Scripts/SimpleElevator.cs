using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleElevator : MonoBehaviour
{
    public Transform LeftDoors;
    public Transform RightDoors;
    public AnimationCurve OpenCloseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    public bool entered = false;

    private int targetLevel = 0;
    float closeProgress = 1;
    private bool closeElevator = true;
    private bool isOnDestination = true;

    private Vector3 initLeftDPos;
    private Vector3 leftOpenedPos;
    private Vector3 initRightDPos;
    private Vector3 rightOpenedPos;
    private Vector3 initPos;

    private void Start()
    {
        initLeftDPos = LeftDoors.localPosition;
        // From world to local position
        leftOpenedPos = transform.InverseTransformPoint(LeftDoors.position - transform.right * 2f);
        initRightDPos = RightDoors.localPosition;
        rightOpenedPos = transform.InverseTransformPoint(RightDoors.position + transform.right * 2f);
        initPos = transform.position;
    }

    private void Update()
    {
        Vector3 singleLevelHeight = new Vector3(0, 6, 0);
        Vector3 currentTargetPos = initPos + singleLevelHeight * targetLevel;

        float dist = Vector3.Distance(transform.position, currentTargetPos);
        if (dist < 0.01f) isOnDestination = true; else isOnDestination = false;

        if (closeProgress > 0.95f)
            transform.position = Vector3.MoveTowards(transform.position, currentTargetPos, Time.deltaTime * 4f);

        if (entered && isOnDestination && closeProgress < 0.1f)
        {
            if (Input.GetKey(KeyCode.Alpha0)) targetLevel = 0;
            if (Input.GetKey(KeyCode.Alpha1)) targetLevel = 1;
            if (Input.GetKey(KeyCode.Alpha2)) targetLevel = 2;
            if (Input.GetKey(KeyCode.Alpha3)) targetLevel = 3;
            if (Input.GetKey(KeyCode.Alpha4)) targetLevel = 4;
            if (Input.GetKey(KeyCode.Alpha5)) targetLevel = 5;
            if (Input.GetKey(KeyCode.Alpha6)) targetLevel = 6;
            if (Input.GetKey(KeyCode.Alpha7)) targetLevel = 7;
            if (Input.GetKey(KeyCode.Alpha8)) targetLevel = 8;
            if (Input.GetKey(KeyCode.Alpha9)) targetLevel = 9;
        }

        AnimateDoors();
    }

    void AnimateDoors()
    {
        if (isOnDestination == false)
        {
            closeElevator = true;
        }
        else
        {
            if (entered)
                closeElevator = false;
            else
                closeElevator = true;
        }

        if (closeElevator)
        {
            if (closeProgress < 1f) closeProgress += Time.deltaTime * 0.7f;
        }
        else
        {
            if (closeProgress > 0f) closeProgress -= Time.deltaTime * 0.7f;
        }

        LeftDoors.transform.localPosition = Vector3.LerpUnclamped(leftOpenedPos, initLeftDPos, OpenCloseCurve.Evaluate(closeProgress));
        RightDoors.transform.localPosition = Vector3.LerpUnclamped(rightOpenedPos, initRightDPos, OpenCloseCurve.Evaluate(closeProgress));
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            other.transform.SetParent(transform, true);
            entered = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Player")
        {
            other.transform.SetParent(null, true);
            entered = false;
        }
    }

    private void OnGUI()
    {
        if (entered)
        {
            Rect drawPos = new Rect(30, 20, 600, 300);
            GUI.Label(drawPos, "Press 0,1,2,3... to send elevator to this floor level");
            drawPos.position += Vector2.up * 20;
            GUI.Label(drawPos, "Current Target Level: " + targetLevel);
        }
    }
}
