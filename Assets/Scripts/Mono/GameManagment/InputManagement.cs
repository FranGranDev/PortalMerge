using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputManagement : MonoBehaviour
{
    #region Singletone
    public static InputManagement Active { get; private set; }
    public InputManagement() => Active = this;
    #endregion


    public enum ActionType
    {
        NotStarted,
        OnCube,
        Interact,
        OnSwipe
    }
    [SerializeField] private ActionType CurrantAction;
    private TapInfo CurrantTap;
    public static Vector3 GetListenerPoint()
    {
        if (Active.CurrantCube != null && !Active.CurrantCube.isNull)
        {
            return Active.CurrantCube.CubeTransform.position;
        }
        else if(Active.prevCube != null && !Active.prevCube.isNull)
        {
            return Active.prevCube.CubeTransform.position;
        }

        return new TapInfo(ActionType.OnSwipe, ScreenCenter(), false).Point;
    }
    public static Vector2 ScreenCenter()
    {
        return new Vector2(Screen.width / 2, Screen.height / 2);
    }
    public float[] GameZoneX { get; private set; }
    public bool OutOfGameZone(Transform obj)
    {
        return obj.position.x < GameZoneX[0] || obj.position.x > GameZoneX[1];
    }
    public bool BelowGameZone(Transform obj)
    {
        return obj.transform.position.y + 1 < CameraTrigger.position.y;
    }
    private Transform CameraTrigger;

    private bool FollowCube;
    private Vector3 TakeDelta;

    private bool Touched;
    public static Vector2 LastTouch;

    private Vector3 StartTapPoint;
    #region Callbacks
    public delegate void OnSwipeInput(Vector3 Point, Vector3 StartPoint);
    public static OnSwipeInput OnCubeThrow;
    public static OnSwipeInput OnCubeFollow;
    public static OnSwipeInput OnCubePlatformFollow;
    public static OnSwipeInput OnSwipeMove;
    public static OnSwipeInput OnSwipeEnd;
    public delegate void OnTouchAction();
    public OnTouchAction OnTakeCube;
    public OnTouchAction OnInteract;
    #endregion

    private ICube CurrantCube;
    private ICube prevCube;

    private void CubeMovement()
    {
        bool MoveCube = true;
        if(!GameManagement.MainData.Static)
        {
            Vector2 Center = ScreenCenter();
            float Offset = ((CurrantTap.InputPos - Center).y) / (Screen.height / 2);
            float AirRatio = CurrantCube.NoInput ? 0f : 0.75f;

            Vector2 CenterLinePos = new Vector2(CurrantTap.InputPos.x, Screen.height / 2);
            TapInfo CenterLine = new TapInfo(CurrantAction, CenterLinePos);

            if ((CurrantTap.InputPos - Center).y > GameManagement.MainData.FollowCubeDeadZoneUp * Screen.height / 2)
            {
                if ((CurrantTap.InputDir.normalized.y > 0f || (CurrantCube.NoInput && TapInfo.PrevDir.y > 0f)) && CurrantCube.CubeTransform.position.z + 2 > CenterLine.Point.z)
                {
                    OnCubeFollow?.Invoke(new Vector3(0, 0, Offset * AirRatio), CurrantTap.Point);
                    Vector2 UpLinePos = new Vector2(CurrantTap.InputPos.x, Screen.height / 2 * (1 + GameManagement.MainData.FollowCubeDeadZoneUp));
                    CurrantTap = new TapInfo(CurrantAction, UpLinePos);
                    MoveCube = CurrantCube.CubeTransform.position.z < CurrantTap.Point.z + 1;
                }
                else
                {
                    OnCubeFollow?.Invoke(Vector3.zero, CurrantTap.Point);
                }
            }
            else if ((CurrantTap.InputPos - Center).y * AirRatio < GameManagement.MainData.FollowCubeDeadZoneDown * Screen.height / 2)
            {
                if ((CurrantTap.InputDir.normalized.y < -0f || (CurrantCube.NoInput && TapInfo.PrevDir.y < -0f)) && CurrantCube.CubeTransform.position.z - 2 < CenterLine.Point.z)
                {
                    OnCubeFollow?.Invoke(new Vector3(0, 0, Offset * AirRatio), CurrantTap.Point);
                    Vector2 DownLinePos = new Vector2(CurrantTap.InputPos.x, Screen.height / 2 * (1 + GameManagement.MainData.FollowCubeDeadZoneDown));
                    CurrantTap = new TapInfo(CurrantAction, DownLinePos);
                    MoveCube = CurrantCube.CubeTransform.position.z > CurrantTap.Point.z - 1;
                }
                else
                {
                    OnCubeFollow?.Invoke(Vector3.zero, CurrantTap.Point);
                }
            }
            else
            {
                OnCubeFollow?.Invoke(Vector3.zero, CurrantTap.Point);
            }
        }
        
        if (MoveCube && !CurrantCube.AfterMerge)
        {
            CurrantCube.Drag(CurrantTap.Point + TakeDelta);
        }
    }
    private void TryTakeCube(GameObject obj)
    {
        CurrantCube = obj.GetComponent<ICube>();
        if (CurrantCube != null)
        {
            CurrantCube.Take();
            SubscribeForCube();

            TakeDelta = CurrantCube.CubeTransform.position - CurrantTap.Point;

            prevCube = CurrantCube;
        }
    }
    private void ThrowCube()
    {
        if (CurrantCube != null)
        {
            UnsubscribeForCube();
            CurrantCube.Throw();
            CurrantCube = null;
        }
    }

    private void TryInteract(GameObject obj)
    {
        IInteract interact = obj.GetComponent<IInteract>();
        if(interact != null)
        {
            interact.DoAction();
            OnInteract?.Invoke();
        }
    }

    private void TryFollowCubeOnPlatform()
    {
        if (prevCube != null && !prevCube.isNull && prevCube.isOnPlatform)
        {
            OnCubePlatformFollow?.Invoke(prevCube.CubeTransform.position, Vector3.zero);
        }
    }

    public void SubscribeForCube(ICube cube)
    {
        StartCoroutine(SubscibeForCubeCour(cube, Time.fixedDeltaTime));
    }
    private IEnumerator SubscibeForCubeCour(ICube cube, float Delay)
    {
        yield return new WaitForSeconds(Delay);
        if (CurrantAction != ActionType.OnCube)
            yield break;
        CurrantCube = cube;
        SubscribeForCube();
        CurrantAction = ActionType.OnCube;
        yield break;
    }

    private void SubscribeForCube()
    {
        if (CurrantCube != null)
        {
            CurrantCube.SubscribeForDestroyed(OnLostCube);
            CurrantCube.SubscribeForEnterPortal(OnLostCube);
            CurrantCube.SubscribeForLeaveGround(OnLostCube);
        }
    }
    private void UnsubscribeForCube()
    {
        if (CurrantCube != null)
        {
            CurrantCube.SubscribeForDestroyed(OnLostCube, true);
            CurrantCube.SubscribeForEnterPortal(OnLostCube, true);
            CurrantCube.SubscribeForMerge(OnLostCube, true);
            CurrantCube.SubscribeForLeaveGround(OnLostCube, true);
        }
    }
    private void OnLostCube(ICube cube)
    {
        CurrantCube = null;
        CurrantAction = ActionType.NotStarted;
    }
    private void OnLostCube(ICube cube1, ICube cube2)
    {
        CurrantCube = null;
        CurrantAction = ActionType.NotStarted;
    }


    private void OnTap()
    {
        CurrantAction = CurrantTap.TapActionInfo;
        switch (CurrantAction)
        {
            case ActionType.NotStarted:

                break;
            case ActionType.Interact:
                TryInteract(CurrantTap.gameObject);
                break;
            case ActionType.OnCube:
                TryTakeCube(CurrantTap.gameObject);
                CurrantTap = new TapInfo(ActionType.OnSwipe);
                StartTapPoint = CurrantTap.Point;

                OnTakeCube?.Invoke();
                break;
            case ActionType.OnSwipe:
                CurrantTap = new TapInfo(ActionType.OnSwipe);
                StartTapPoint = CurrantTap.Point;

                prevCube = null;
                break;
        }
    }
    private void OnTapEnded()
    {
        switch (CurrantAction)
        {
            case ActionType.OnSwipe:
                OnSwipeEnd?.Invoke(CurrantTap.Point, StartTapPoint);
                break;
            case ActionType.OnCube:
                OnCubeThrow?.Invoke(CurrantTap.Point, TakeDelta);
                if (CurrantCube != null && !CurrantCube.isNull)
                {
                    CurrantCube.Throw();
                }
                TakeDelta = Vector3.zero;
                break;
        }
        CurrantAction = ActionType.NotStarted;
    }

    private void Movement()
    {
        CurrantTap = new TapInfo(CurrantAction);

        if (Application.isMobilePlatform)
        {
            if (!Touched && Input.touchCount > 0)
            {
                OnTap();
                Touched = true;
            }
            if (Touched && Input.touchCount == 0)
            {
                OnTapEnded();
                Touched = false;
            }
        }
        else
        {
            if (Input.GetMouseButtonDown(0))
            {
                OnTap();
            }
            if (Input.GetMouseButtonUp(0))
            {
                OnTapEnded();
            }
        }
    }
    private void ActionExecute()
    {
        switch (CurrantAction)
        {
            case ActionType.NotStarted:
                {


                }
                break;
            case ActionType.OnCube:
                {
                    if (CurrantCube != null && !CurrantCube.isNull)
                    {
                        CubeMovement();
                    }
                }
                break;
            case ActionType.OnSwipe:
                {
                    OnSwipeMove?.Invoke(CurrantTap.Point, StartTapPoint);
                }
                break;
        }
    }
    private void FixedActionExecute()
    {
        if (CurrantAction == ActionType.NotStarted)
        {
            TryFollowCubeOnPlatform();
        }
    }

    private void Init()
    {
        GameZoneX = new float[2];
        GameZoneX[0] = new TapInfo(ActionType.OnSwipe, new Vector2(0, 0)).Point.x - 1;
        GameZoneX[1] = new TapInfo(ActionType.OnSwipe, new Vector2(Screen.width, 0)).Point.x + 1;

        CameraTrigger = GameObject.FindGameObjectWithTag("CameraTrigger").transform;
    }
    private void Start()
    {
        Init();
    }
    private void Update()
    {
        if (GameManagement.isGameStarted)
        {
            Movement();
            ActionExecute();
        }
    }
    private void FixedUpdate()
    {
        if (GameManagement.isGameStarted)
        {
            FixedActionExecute();
        }
    }


    public class TapInfo
    {
        public readonly Vector3 Point;
        public readonly Vector2 InputPos;
        public readonly Vector2 InputDir;
        public readonly string Tag;
        public readonly GameObject gameObject;
        public readonly ActionType TapActionInfo;

        public static Vector3 PrevPoint;
        private static Vector2 PrevTouch;
        public static Vector3 PrevDir;
        public const string NULL_TAG = "NULL";
        private ActionType TagToActionInfo(string Tag)
        {
            switch (Tag)
            {
                case NULL_TAG:
                    return ActionType.NotStarted;
                case "Cube":
                    return ActionType.OnCube;
                case "CameraInteract":
                    return ActionType.Interact;
                default:
                    return ActionType.OnSwipe;
            }
        }
        private LayerMask GetMask(ActionType info)
        {
            switch (info)
            {
                case ActionType.OnCube:
                    return LayerMask.GetMask(new string[2] { "Camera", "Ground" });
                case ActionType.OnSwipe:
                    return LayerMask.GetMask(new string[1] { "Camera" });
                default:
                    return LayerMask.GetMask(new string[3] { "Camera", "Cube", "Object" });
            }

        }

        public TapInfo(ActionType NowClickInfo)
        {
            if (Application.isMobilePlatform)
            {
                if (Input.touchCount > 0)
                {
                    InputPos = Input.GetTouch(0).position;
                    LastTouch = InputPos;

                }
                else
                {
                    InputPos = LastTouch;
                }
            }
            else
            {
                InputPos = Input.mousePosition;
            }
            if ((InputPos - PrevTouch).magnitude > 0.25f)
            {
                InputDir = (InputPos - PrevTouch).normalized;
                PrevDir = InputDir;
            }
            else
            {
                //InputDir = PrevDir;
                InputDir = Vector2.zero;
            }

            PrevTouch = InputPos;

            Ray ray = Camera.main.ScreenPointToRay(InputPos);
            if (Physics.Raycast(ray, out RaycastHit raycastHit, 250f, GetMask(NowClickInfo)))
            {
                Point = raycastHit.point;
                Tag = raycastHit.transform.tag;
                PrevPoint = Point;
                gameObject = raycastHit.transform.gameObject;
            }
            else
            {
                gameObject = null;
                Point = PrevPoint;
                Tag = NULL_TAG;
            }

            TapActionInfo = TagToActionInfo(Tag);
        }
        public TapInfo(ActionType NowClickInfo, Vector2 InputPos, bool SetPrevTouch = true, bool ForceSetInputDir = false)
        {
            if (ForceSetInputDir)
            {
                InputDir = PrevDir;
            }
            else
            {
                InputDir = InputPos.normalized;
            }

            if (SetPrevTouch)
            {
                PrevTouch = InputPos;
            }
            Ray ray = Camera.main.ScreenPointToRay(InputPos);
            if (Physics.Raycast(ray, out RaycastHit raycastHit, 250f, GetMask(NowClickInfo)))
            {
                Point = raycastHit.point;
                Tag = raycastHit.transform.tag;
                PrevPoint = Point;
                if (raycastHit.transform.childCount > 0)
                {
                    gameObject = raycastHit.transform.GetChild(0).gameObject;
                }
                else
                {
                    gameObject = raycastHit.transform.gameObject;
                }
            }
            else
            {
                gameObject = null;
                Point = PrevPoint;
                Tag = NULL_TAG;
            }

            TapActionInfo = TagToActionInfo(Tag);
        }
    }
}