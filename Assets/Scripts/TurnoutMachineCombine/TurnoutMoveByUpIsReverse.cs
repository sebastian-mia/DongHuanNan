using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TurnoutMoveByUpIsReverse : MonoBehaviour
{
    public float turnoutDistance;
    public Button normalPositionButton;
    public Button reversePositionButton;
    private Vector3 currentTransform;
    private Vector3 targetTransform;
    private int turnoutIsNormalPositionIndex = 0;
    private int turnoutIsReversePositionIndex = 1;
    public float duration;
    private bool moveIsOver = true;

    void Start()
    {
        currentTransform = new Vector3(
            transform.position.x,
            transform.position.y,
            transform.position.z
        );
        targetTransform = new Vector3(
            transform.position.x,
            transform.position.y,
            transform.position.z
        );
        normalPositionButton.onClick.AddListener(SwitchTurnoutToNormalPosition);
        reversePositionButton.onClick.AddListener(SwitchTurnoutToReversePosition);
    }

    void Update() { }

    private void SwitchTurnoutToNormalPosition()
    {
        if (turnoutIsNormalPositionIndex < 1 && moveIsOver)
        {
            moveIsOver = false;
            StartCoroutine("ISwitchoNormalPosition");
            turnoutIsNormalPositionIndex++;
            turnoutIsReversePositionIndex = 0;
        }
    }

    private void SwitchTurnoutToReversePosition()
    {
        if (turnoutIsReversePositionIndex < 1 && moveIsOver)
        {
            moveIsOver = false;
            StartCoroutine("ISwitchReversePosition");
            turnoutIsReversePositionIndex++;
            turnoutIsNormalPositionIndex = 0;
        }
    }

    private IEnumerator ISwitchReversePosition()
    {
        currentTransform = new Vector3(
            transform.position.x+ turnoutDistance,
            transform.position.y,
            transform.position.z
        );
        targetTransform = new Vector3(
            transform.position.x,
            transform.position.y,
            transform.position.z 
        );
        Debug.Log("targetTransform:" + targetTransform);
        float moveTime = 0;
        while (moveTime < 1)
        {
            moveTime += Time.deltaTime / duration;
            transform.position = Vector3.Lerp(currentTransform, targetTransform, moveTime);
            yield return null; // 等下一帧
        }

        transform.position = targetTransform;
        moveIsOver = true;
        Debug.Log("moveIsOver:" + moveIsOver);
    }

    private IEnumerator ISwitchoNormalPosition()
    {
        currentTransform = new Vector3(
            transform.position.x,
            transform.position.y,
            transform.position.z
        );
        targetTransform = new Vector3(
            transform.position.x- turnoutDistance,
            transform.position.y,
            transform.position.z 
        );
        float moveTime = 0;
        while (moveTime < 1)
        {
            moveTime += Time.deltaTime / duration;
            transform.position = Vector3.Lerp(currentTransform, targetTransform, moveTime);
            yield return null; // 等下一帧
        }

        transform.position = targetTransform;
        moveIsOver = true;
        Debug.Log("moveIsOver:" + moveIsOver);
    }
}
