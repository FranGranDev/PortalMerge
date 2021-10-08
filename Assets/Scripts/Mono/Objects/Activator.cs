using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Activator : MonoBehaviour
{
    [SerializeField] private GameObject _target;
    [SerializeField] private IActivate Target;
    [SerializeField] private bool Disactivate;
    [SerializeField] private bool ActiveOnes;
    public bool _activated { get; private set; }
    private float DelayTime;
    private Coroutine ActivateCoroutine;
    [Header("Components")]
    private const string ANIM_BOOL = "Active";
    private Animator _anim;
    private bool HaveAnimator;

    private void Init()
    {
        if (_target != null && _target.GetComponent<IActivate>() != null)
        {
            Target = _target.GetComponent<IActivate>();
        }
        DelayTime = GameManagement.MainData.DelayTime;
        HaveAnimator = transform.TryGetComponent<Animator>(out _anim);
    }

    private void Activate(bool on)
    {
        if (Target == null)
            return;
        if(ActivateCoroutine != null)
        {
            StopCoroutine(ActivateCoroutine);
        }
        ActivateCoroutine = StartCoroutine(ActivateCour(on));
    }
    private IEnumerator ActivateCour(bool on)
    {
        yield return new WaitForSeconds(DelayTime);
        Target.Activate(Disactivate ? !on : on);
        _activated = on;
        if (HaveAnimator)
        {
            _anim.SetBool(ANIM_BOOL, on);
        }
        ActivateCoroutine = null;
        yield break;
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
