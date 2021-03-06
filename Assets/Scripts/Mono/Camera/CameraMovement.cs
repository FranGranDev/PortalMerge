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
    private Vector3 Velocity;
    public const float MAX_SPEED = 100;
    private bool isMoving;
    private Vector3 PrevPos;
    private Vector3 StartPosition;
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
        if (GameManagement.MainData.Static)
            return;
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
        if (GameManagement.MainData.Static)
            return;
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
        if(MoveToCoroutine != null)
        {
            StopCoroutine(MoveToCoroutine);
        }
        StartCoroutine(MoveToOnWinCour());
        StartCoroutine(ConfittiCour(GameManagement.LastCube));
    }
    private IEnumerator MoveToOnWinCour()
    {
        yield return new WaitForFixedUpdate();
        ICube cube = GameManagement.LastCube;
        while (GameManagement.isGameWin)
        {
            Vector3 newPoint = new Vector3(cube.CubeTransform.position.x, cube.CubeTransform.position.y, cube.CubeTransform.position.z) + GameManagement.MainData.CameraWinOffset;
            transform.position = Vector3.Lerp(transform.position, newPoint, GameManagement.MainData.MoveToCubeOnWinSpeed * 0.1f);
            yield return new WaitForFixedUpdate();
        }
        yield break;
    }
    private IEnumerator ConfittiCour(ICube cube)
    {
        yield return new WaitForSeconds(0.5f);
        int ConfittiNum = Random.Range(20, 25);
        for (int i = 0; i < ConfittiNum && !cube.isNull && GameManagement.isGameWin; i++)
        {
            float Lenght = Random.Range(3f, 6f);
            Vector3 Direction = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
            Vector3 Position = cube.CubeTransform.position + Direction * Lenght + Vector3.up * 2;

            GameObject particle = Instantiate(GameManagement.MainData.GetConfetti(), Position, Quaternion.identity, GameManagement.Active.GetLevelTransform);
            particle.transform.localScale = Vector3.one * GameManagement.MainData.ConfittiParticleSize;
            yield return new WaitForSeconds(Random.Range(0.025f, 0.15f));
        }
    }

    public void FollowCubeDie(ICube cube)
    {
        if(MoveToCoroutine != null)
        {
            StopCoroutine(MoveToCoroutine);
        }
        MoveToCoroutine = StartCoroutine(FollowCubeDieCour(cube));
    }
    private IEnumerator FollowCubeDieCour(ICube cube)
    {
        while(cube != null && !cube.isNull && !cube.isDestroyed && InputManagement.Active.OutOfGameZone(cube.CubeTransform))
        {
            Vector3 newPoint = new Vector3(cube.CubeTransform.position.x, transform.position.y, cube.CubeTransform.position.z);
            transform.position = Vector3.Lerp(transform.position, newPoint, 0.05f);
            yield return new WaitForFixedUpdate();
        }
        if(cube != null && !cube.isNull && !cube.isDestroyed)
        {
            StartCoroutine(MoveToCenterCour());
        }
        else
        {
            MoveToCoroutine = null;
        }

        yield break;
    }
    private IEnumerator MoveToCenterCour()
    {
        while((transform.position - StartPosition).magnitude > 0.1f)
        {
            transform.position = Vector3.Lerp(transform.position, StartPosition, 0.075f);
            yield return new WaitForFixedUpdate();
        }
        transform.position = StartPosition;
        yield break;
    }

    private void MoveToCube(ICube cube)
    {
        if (GameManagement.MainData.Static)
            return;
        if (MoveToCoroutine != null)
        {
            StopCoroutine(MoveToCoroutine);
        }
        MoveToCoroutine = StartCoroutine(MoveToCour(cube));
    }

    private IEnumerator MoveToCour(ICube cube)
    {
        Vector3 newPoint = new Vector3(GameManagement.MainData.LockSideMove ? 0 : cube.CubeTransform.position.x, transform.position.y, cube.CubeTransform.position.z);
        while (!cube.isNull && Mathf.Abs(transform.position.z - cube.CubeTransform.position.z) > 0.1f)
        {
            newPoint = new Vector3(GameManagement.MainData.LockSideMove ? 0 : cube.CubeTransform.position.x, transform.position.y, cube.CubeTransform.position.z);
            transform.position = Vector3.Lerp(transform.position, newPoint, GameManagement.MainData.MoveToPortalSpeed * 0.1f);
            yield return new WaitForFixedUpdate();
        }
        if(cube.isNull)
        {
            while (Mathf.Abs(transform.position.z - newPoint.z) > 0.1f)
            {
                transform.position = Vector3.Lerp(transform.position, newPoint, GameManagement.MainData.MoveToPortalSpeed * 0.1f);
                yield return new WaitForFixedUpdate();
            }
        }
        transform.position = new Vector3(0, transform.position.y, transform.position.z);
        yield break;
    }
    private void CalculateSpeed()
    {
        CameraSpeed = (transform.position - PrevPos).magnitude / Time.deltaTime;
        PrevPos = transform.position;

    }

    private void CameraSimpleFollowCube(Vector3 Point, Vector3 StartPoint)
    {
        if (GameManagement.MainData.Static)
            return;
        Vector3 newPoint = new Vector3(0, transform.position.y, Point.z);
        transform.position = Vector3.Lerp(transform.position, newPoint, 0.05f);

        Velocity = Vector3.forward * CameraSpeed * 0.1f;
    }

    private void CameraFollowCube(Vector3 Speed, Vector3 Point)
    {
        if (GameManagement.MainData.Static)
            return;
        float StopRatio = Speed.z == 0 ? 3 : 1f;
        Velocity = Vector3.Lerp(Velocity, Speed, StopRatio * Time.deltaTime);

        isMoving = true;
        Point = transform.position + Velocity * 5;

        transform.position = Vector3.Lerp(transform.position, Point, CameraCubeFollowSpeed * Time.deltaTime);
    }
    private void CameraStopFollowCube(Vector3 Point, Vector3 CubeDelta)
    {
        if (GameManagement.MainData.Static)
            return;
        if (MoveToCoroutine != null)
        {
            StopCoroutine(MoveToCoroutine);
        }
        MoveToCoroutine = StartCoroutine(CameraStopFollowCubeCour());
    }
    private IEnumerator CameraStopFollowCubeCour()
    {
        while(Mathf.Abs(Velocity.z) > 0.01f)
        {
            Velocity.z *= (1 - GameManagement.MainData.FollowCubeFriction);
            Vector3 Point = transform.position + Velocity * 5;

            transform.position = Vector3.Lerp(transform.position, Point, 0.05f);
            yield return new WaitForFixedUpdate();
        }
        Velocity = Vector3.zero;
        MoveToCoroutine = null;
        yield break;
    }

    public void SubcribeToCube(ICube cube, bool Unsubscribe = false)
    {
        cube.SubscribeForExitPortal(MoveToCube, Unsubscribe);
    }

    private void Update()
    {
        CalculateSpeed();   
    }

    private void Init()
    {
        active = this;
        StartPosition = transform.position;
    }

    private void Awake()
    {
        Init();
    }
    private void Start()
    {
        ApplySettings();


        InputManagement.OnSwipeMove += CameraSwipeMove;
        InputManagement.OnSwipeEnd += CameraSwipeEnd;
        InputManagement.OnCubeFollow += CameraFollowCube;
        InputManagement.OnCubeThrow += CameraStopFollowCube;
        InputManagement.OnCubePlatformFollow += CameraSimpleFollowCube;

        GameManagement.OnGameWin += CameraWinMove;
    }
    private void OnDisable()
    {
        InputManagement.OnSwipeMove -= CameraSwipeMove;
        InputManagement.OnSwipeEnd -= CameraSwipeEnd;
        InputManagement.OnCubeFollow -= CameraFollowCube;
        InputManagement.OnCubeThrow -= CameraStopFollowCube;
        InputManagement.OnCubePlatformFollow -= CameraSimpleFollowCube;

        GameManagement.OnGameWin -= CameraWinMove;
    }
}
