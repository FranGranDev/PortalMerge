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
    [SerializeField] protected Rigidbody _rig;

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
    private void OnDisable()
    {
        if (ActivatorTarget != null)
        {
            ActivatorTarget.Subscribe(Activate, true);
        }
    }

    private void Update()
    {
        
    }
}
