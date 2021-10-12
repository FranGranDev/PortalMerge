using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    public static CameraMovement active { get; private set; }

    private float CameraCubeFollowSpeed;
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
    private Coroutine MoveToCoroutine;

    private void ApplySettings()
    {
        PrevPos = transform.position;

        CameraCubeFollowSpeed = GameManagement.MainData.FollowCubeSpeed;
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

        if(MoveToCoroutine != null)
        {
            StopCoroutine(MoveToCoroutine);
        }
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

    private void CameraWinMove()
    {
        MoveToCube(GameManagement.LastCube);
    }

    private void MoveToCube(ICube cube)
    {
        Vector3 Point = cube.CubeTransform.position + Vector3.forward * -5;
        if(MoveToCoroutine != null)
        {
            StopCoroutine(MoveToCoroutine);
        }
        MoveToCoroutine = StartCoroutine(MoveToCour(Point));
    }
    private IEnumerator MoveToCour(Vector3 Target)
    {
        while (Mathf.Abs(transform.position.z - Target.z) > 0.25f)
        {
            Vector3 newPoint = new Vector3(GameManagement.MainData.LockSideMove ? transform.position.x : Target.x, transform.position.y, Target.z);
            transform.position = Vector3.Lerp(transform.position, newPoint, GameManagement.MainData.MoveToPortalSpeed * 0.1f);
            yield return new WaitForFixedUpdate();
        }
        yield break;
    }
    private void CalculateSpeed()
    {
        CameraSpeed = (transform.position - PrevPos).magnitude / Time.deltaTime;
        PrevPos = transform.position;

    }

    private void CameraFollowCube(Vector3 Point, Vector3 StartPoint)
    {
        isMoving = true;
        Point = new Vector3(Point.x * SideMove(), transform.position.y, Point.z);

        transform.position = Vector3.Lerp(transform.position, Point, CameraCubeFollowSpeed * Time.deltaTime);

        if (MoveToCoroutine != null)
        {
            StopCoroutine(MoveToCoroutine);
        }
    }

    public void SubcribeToCube(ICube cube, bool Unsubscribe = false)
    {
        cube.SubscribeForEnterPortal(MoveToCube, Unsubscribe);
    }

    private void Update()
    {
        CalculateSpeed();   
    }

    private void Awake()
    {
        active = this;
    }
    private void Start()
    {
        ApplySettings();


        InputManagement.OnSwipeMove += CameraSwipeMove;
        InputManagement.OnSwipeEnd += CameraSwipeEnd;
        InputManagement.OnCubeFollow += CameraFollowCube;

        GameManagement.OnGameWin += CameraWinMove;
    }
    private void OnDisable()
    {
        InputManagement.OnSwipeMove -= CameraSwipeMove;
        InputManagement.OnSwipeEnd -= CameraSwipeEnd;
        InputManagement.OnCubeFollow -= CameraFollowCube;

        GameManagement.OnGameWin -= CameraWinMove;
    }
}
