using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    [SerializeField] private float CameraFollowSpeed;
    [Range(1, 25)]
    [SerializeField] private float CameraFriction;
    private Camera _camera;
    private float CameraSpeed;
    public const float MAX_SPEED = 100;
    private bool isMoving;
    private Vector3 PrevPos;

    private void CameraSwipeMove(Vector3 Point, Vector3 StartPoint)
    {
        isMoving = true;
        Point = new Vector3(Point.x, 0, Point.z);
        StartPoint = new Vector3(StartPoint.x, 0, StartPoint.z);
        Vector3 Direction = StartPoint - Point;
        Vector3 CurrantOffset = Direction + transform.position;

        transform.position = Vector3.Lerp(transform.position, CurrantOffset, CameraFollowSpeed * Time.deltaTime);
    }
    private void CameraSwipeEnd(Vector3 Point, Vector3 StartPoint)
    {
        isMoving = false;
        Point = new Vector3(Point.x, 0, Point.z);
        StartPoint = new Vector3(StartPoint.x, 0, StartPoint.z);
        Vector3 Direction = (StartPoint - Point).normalized;

        StartCoroutine(CameraEndFlyCour(Direction, CameraSpeed > MAX_SPEED ? MAX_SPEED : CameraSpeed));
    }
    private IEnumerator CameraEndFlyCour(Vector3 Direction, float StartSpeed)
    {
        float Speed = StartSpeed;
        while(!isMoving && Speed > 0f)
        {
            Vector3 Offset = transform.position + Direction * Mathf.Sqrt(Speed);
            transform.position = Vector3.Lerp(transform.position, Offset, Speed / StartSpeed * 0.1f);
            Speed -= CameraFriction;
            yield return new WaitForFixedUpdate();
        }
        yield break;
    }
    private void CalculateSpeed()
    {
        CameraSpeed = (transform.position - PrevPos).magnitude / Time.deltaTime;
        PrevPos = transform.position;

    }


    private void Update()
    {
        CalculateSpeed();   
    }

    private void Awake()
    {
        InputManagement.OnSwipeMove += CameraSwipeMove;
        InputManagement.OnSwipeEnd += CameraSwipeEnd;

        PrevPos = transform.position;
    }
    private void OnDisable()
    {
        InputManagement.OnSwipeMove -= CameraSwipeMove;
        InputManagement.OnSwipeEnd -= CameraSwipeEnd;
    }
}
