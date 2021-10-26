using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Activator : MonoBehaviour
{
    //2 реализации: либо давать ссылку на объект Target, либо в класе объекта подисаться на событие OnActivate

    [SerializeField] private GameObject[] _target;
    private List<IActivate> Target;
    [SerializeField] private bool Disactivate;
    [SerializeField] private bool ActiveOnes;
    public bool _activated { get; private set; }
    private float DelayTime;
    private Coroutine ActivateCoroutine;

    public delegate void OnActivateEvent(bool on);
    private OnActivateEvent OnActivate; 
    public void Subscribe(OnActivateEvent action, bool unsubscribe = false)
    {
        if(unsubscribe)
        {
            OnActivate -= action;
        }
        else
        {
            OnActivate += action;
        }
    }

    [Header("Components")]
    private const string ANIM_BOOL = "Active";
    private Animator _anim;
    private MeshRenderer _render;
    private bool HaveAnimator;
    [Header("Settings")]
    [SerializeField] private Material Red;
    [SerializeField] private Material Green;

    private void Init()
    {
        Target = new List<IActivate>();
        for(int i = 0; i < _target.Length; i++)
        {
            if (_target[i] == null)
                continue;
            IActivate temp = _target[i]?.GetComponent<IActivate>();
            if(temp != null)
            {
                Target.Add(temp);
            }
        }

        DelayTime = GameManagement.MainData.DelayTime;
        HaveAnimator = transform.TryGetComponent<Animator>(out _anim);
        transform.GetChild(0).TryGetComponent<MeshRenderer>(out _render);
    }

    private void Activate(bool on)
    {
        if(ActivateCoroutine != null)
        {
            StopCoroutine(ActivateCoroutine);
        }
        ActivateCoroutine = StartCoroutine(ActivateCour(on));
    }
    private IEnumerator ActivateCour(bool on)
    {
        yield return new WaitForSeconds(DelayTime);
        foreach(IActivate activate in Target)
        {
            activate?.Activate(Disactivate ? !on : on);
        }
        _activated = on;
        OnActivate?.Invoke(Disactivate ? !on : on);
        if (HaveAnimator)
        {
            _anim.SetBool(ANIM_BOOL, on);
        }
        if(on)
        {
            SoundManagment.PlaySound("button", transform);
        }
        ActivateCoroutine = null;
        yield break;
    }

    public void SetRedColor()
    {
        _render.material = Red;
    }
    public void SetGreenColor()
    {
        _render.material = Green;
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.tag == "Cube")
        {
            if(!_activated)
            {
                Activate(true);
            }
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (ActiveOnes)
            return;
        if (other.tag == "Cube")
        {
            if (_activated)
            {
                Activate(false);
            }
        }
    }

    private void Start()
    {
        Init();
    }
}
public interface IActivate
{
    void Activate(bool on = true);
}
public interface IInteract
{
    void DoAction();
}
