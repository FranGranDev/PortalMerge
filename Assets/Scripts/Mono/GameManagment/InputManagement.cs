using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputManagement : MonoBehaviour
{
    #region Singletone
    private static InputManagement _active;
    public static InputManagement Active { get => _active; }
    public InputManagement() => _active = this;
    #endregion

    private enum ActionType
    {
        NotStarted,
        OnCube,
        OnSwipe
    }
    [SerializeField] private ActionType CurrantAction;
    private TapInfo CurrantTap;

    private bool FollowCube;

    private bool Touched;
    public static Vector2 LastTouch;

    #region Swipe
    private Vector3 StartTapPoint;
    public delegate void OnSwipeInput(Vector3 Point, Vector3 StartPoint);
    public static OnSwipeInput OnCubeFollow;
    public static OnSwipeInput OnSwipeMove;
    public static OnSwipeInput OnSwipeEnd;
    #endregion
    #region Cube drag
    private ICube CurrantCube;
    #endregion

    private class TapInfo
    {
        public readonly Vector3 Point;
        public readonly Vector2 InputPos;
        public readonly string Tag;
        public readonly GameObject gameObject;
        public readonly ActionType TapActionInfo;

        public static Vector3 PrevPoint;
        public const string NULL_TAG = "NULL";
        private ActionType TagToActionInfo(string Tag)
        {
            switch (Tag)
            {
                case NULL_TAG:
                    return ActionType.NotStarted;
                case "Cube":
                    return ActionType.OnCube;
                default:
                    return ActionType.OnSwipe;
            }
        }
        private LayerMask GetMask(ActionType info)
        {
            switch (info)
            {
                case ActionType.OnCube:
                    return LayerMask.GetMask(new string[1] { "Ground" });
                case ActionType.OnSwipe:
                    return LayerMask.GetMask(new string[1] { "Camera" });
                default:
                    return LayerMask.GetMask(new string[3] { "Camera", "Object", "Ground" });
            }

        } //if OnSwipe, chech only "Camera" layer, if OnCube check only "Ground" layers

        public TapInfo(ActionType NowClickInfo)
        {
            if (Application.isMobilePlatform)
            {
                if(Input.touchCount > 0)
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
    }

    private void TryTakeCube(GameObject obj)
    {
        CurrantCube = obj.GetComponent<ICube>();
        if (CurrantCube != null)
        {
            CurrantCube.Take();
            SubscribeForCube();
        }
        else
        {
            Debug.Log("��� ���� �� ���-�� �� ���");
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

    private void SubscribeForCube()
    {
        if(CurrantCube != null)
        {
            CurrantCube.SubscribeForDestroyed(OnLostCube);
            CurrantCube.SubscribeForEnterPortal(OnLostCube);
            CurrantCube.SubscribeForMerge(OnLostCube);
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
        switch(CurrantAction)
        {
            case ActionType.NotStarted:

                break;
            case ActionType.OnCube:
                TryTakeCube(CurrantTap.gameObject);
                CurrantTap = new TapInfo(ActionType.OnSwipe);//����� TapInfo �������� ��� ����, ����� Raycast ��� ������ �� ����������� "Camera"
                StartTapPoint = CurrantTap.Point;
                break;
            case ActionType.OnSwipe:
                CurrantTap = new TapInfo(ActionType.OnSwipe);//����� TapInfo �������� ��� ����, ����� Raycast ��� ������ �� ����������� "Camera"
                StartTapPoint = CurrantTap.Point;
                break;
        }
    }
    private void OnTapEnded()
    {
        switch(CurrantAction)
        {
            case ActionType.OnSwipe:
                OnSwipeEnd?.Invoke(CurrantTap.Point, StartTapPoint);
                break;
            case ActionType.OnCube:
                if(CurrantCube != null)
                {
                    CurrantCube.Throw();
                }
                break;
        }
        CurrantAction = ActionType.NotStarted;
        FollowCube = false;
    }

    private void Movement()
    {
        CurrantTap = new TapInfo(CurrantAction);

        if(Application.isMobilePlatform)
        {
            if (!Touched && Input.touchCount > 0)
            {
                OnTap();
                Touched = true;
            }
            if(Touched && Input.touchCount == 0)
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
                    if (!CurrantCube.isNull)
                    {
                        CurrantCube.Drag(CurrantTap.Point);

                        if((CurrantTap.Point - CurrantCube.CubeTransform.position).magnitude > GameManagement.MainData.FollowCubeMinDistance)
                        {
                            OnCubeFollow?.Invoke(CurrantTap.Point, CurrantCube.CubeTransform.position);
                        }
                        //CurrantTap = new TapInfo(ActionType.OnSwipe);

                        //Vector2 ScreenCenter = new Vector2((float)Screen.width / 2, (float)Screen.height / 2);
                        //if ((ScreenCenter - CurrantTap.InputPos).magnitude > GameManagement.MainData.FollowCubeMinDistance)
                        //{
                        //    OnCubeFollow?.Invoke(CurrantTap.Point, CurrantCube.CubeTransform.position);
                        //}
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

    private void Start()
    {

    }
    private void Update()
    {
        if (GameManagement.isGameStarted)
        {
            Movement();
            ActionExecute();
        }
    }
}