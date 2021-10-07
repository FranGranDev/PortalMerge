using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    private float CameraFollowSpeed;
    private float CameraFriction;
    private bool LockSideMove;
    private float SideMove()
    {
        return LockSideMove ? 0 : 1;
    }

    private Camera _camera;
    private float CameraSpeed;
    public const float MAX_SPEED = 100;
    private bool isMoving;
    private Vector3 PrevPos;

    private void ApplySettings()
    {
        PrevPos = transform.position;

        CameraFollowSpeed = GameManagement.MainData.CameraFollowSpeed;
        CameraFriction = GameManagement.MainData.CameraFriction;
        LockSideMove = GameManagement.MainData.LockSideMove;
    }
    private void CameraSwipeMove(Vector3 Point, Vector3 StartPoint)
    {
        isMoving = true;
        Point = new Vector3(Point.x * SideMove(), 0, Point.z);
        StartPoint = new Vector3(StartPoint.x * SideMove(), 0, StartPoint.z);
        Vector3 Direction = StartPoint - Point;
        Vector3 CurrantOffset = Direction + transform.position;

        transform.position = Vector3.Lerp(transform.position, CurrantOffset, CameraFollowSpeed * Time.deltaTime);
    }
    private void CameraSwipeEnd(Vector3 Point, Vector3 StartPoint)
    {
        isMoving = false;
        Point = new Vector3(Point.x * SideMove(), 0, Point.z);
        StartPoint = new Vector3(StartPoint.x * SideMove(), 0, StartPoint.z);
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

    private void Start()
    {
        ApplySettings();

        InputManagement.OnSwipeMove += CameraSwipeMove;
        InputManagement.OnSwipeEnd += CameraSwipeEnd;

       
    }
    private void OnDisable()
    {
        InputManagement.OnSwipeMove -= CameraSwipeMove;
        InputManagement.OnSwipeEnd -= CameraSwipeEnd;
    }
}
