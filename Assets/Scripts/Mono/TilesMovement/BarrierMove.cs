using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public abstract class BarrierMove : MonoBehaviour, IActivate
{
    [Header("Settings")]
    [SerializeField] protected bool OneMove;
    [SerializeField] protected bool isMoveToEnd;
    [SerializeField] protected bool Activated;
    [SerializeField] private bool MoveToOnStop;
    [SerializeField] protected Activator ActivatorTarget; //2 реализации: ссылка на кнопку либо в кнопке ссылку на этот объект

    [Header("Start Point")]
    [SerializeField] protected Transform StartPoint;
    [SerializeField] protected float StartRoadTime;
    [SerializeField] protected float StartDelay;
    [SerializeField] protected Ease StartEase = Ease.InOutSine;
    [Header("End Point")]
    [SerializeField] protected Transform EndPoint;
    [SerializeField] protected float EndRoadTime;
    [SerializeField] protected float EndDelay;
    [SerializeField] protected Ease EndEase = Ease.InOutSine;

    [Header("Components")]
    [SerializeField] protected BoxCollider _collider;
    public Rigidbody _rig { get; private set; }

    private List<ICube> CubesOn = new List<ICube>();
    private void ClearEmpty()
    {
        foreach(ICube cube in CubesOn)
        {
            if(cube == null || cube.isNull)
            {
                CubesOn.Remove(cube);
            }
        }
    }
    private Dictionary<ICube, Coroutine> OnPlatformCoroutine = new Dictionary<ICube, Coroutine>();

    public void Activate(bool on)
    {
        Activated = on;
        if(on)
        {
            ActionExecute();
        }
        else
        {
            ActionStop();
        }
    }

    private void ActionExecute()
    {
        if(isMoveToEnd)
        {
            MoveToEnd();
        }
        else
        {
            MoveToStart();
        }
    }
    private void ActionStop()
    {
        if (!MoveToOnStop)
        {
            _rig.DOKill();
        }
    }

    protected abstract void MoveToEnd();
    protected abstract void MoveToStart();

    private void Init()
    {
        if (_rig == null) _rig = GetComponent<Rigidbody>();
        if (_collider == null) _collider = GetComponent<BoxCollider>();
        if (ActivatorTarget != null)
        {
            ActivatorTarget.Subscribe(Activate);
        }
        if(StartPoint == null)
        {
            StartPoint = transform;
        }
        if(EndPoint == null)
        {
            EndPoint = transform;
        }
        Activate(Activated);
    }

    private void Start()
    {
        Init();
    }
    private void OnDestroy()
    {
        if (ActivatorTarget != null)
        {
            ActivatorTarget.Subscribe(Activate, true);
        }
        CubesOn.Clear();
    }

    public void OnEnterPlatform(ICube cube)
    {
        if (!CubesOn.Exists(item => item == cube))
        {
            CubesOn.Add(cube);
            if (!OnPlatformCoroutine.ContainsKey(cube))
            {
                OnPlatformCoroutine.Add(cube, StartCoroutine(OnEnterPlatformCour(cube)));
            }
        }
    }
    private IEnumerator OnEnterPlatformCour(ICube cube)
    {
        while (cube.isNull || cube.CubeRig.angularVelocity.magnitude > 0.25f)
        {
            if (cube.isNull)
            {
                CubesOn.Remove(cube);
                yield break;
            }
                
            yield return new WaitForFixedUpdate();
        }
        yield return new WaitForFixedUpdate();
        Debug.Log("Enter");
        cube.EnterPlatform(this);

        OnPlatformCoroutine.Remove(cube);
        yield break;
    }

    public void OnExitPlatform(ICube cube)
    {
        if (CubesOn.Exists(item => item == cube))
        {
            StartCoroutine(OnExitPlatformCour(cube));
        }
    }
    private IEnumerator OnExitPlatformCour(ICube cube)
    {
        while (cube.isNull || cube.isMoving || cube.CubeRig.angularVelocity.magnitude > 0.25f)
        {
            if (cube.isNull)
            {
                CubesOn.Remove(cube);
                yield break;
            }
                
            yield return new WaitForFixedUpdate();
        }

        if (OnPlatformCoroutine.ContainsKey(cube))
        {
            StopCoroutine(OnPlatformCoroutine[cube]);
            OnPlatformCoroutine.Remove(cube);
        }
        Debug.Log("exit");

        CubesOn.Remove(cube);
        yield break;
    }

    private void OnTriggerStay(Collider other)
    {
        if(other.tag == "Cube")
        {
            ICube cube = other.GetComponent<ICube>();
            if(cube != null)
            {
                OnEnterPlatform(cube);
            }
        }
    }


    private void Update()
    {
        
    }
}
