using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Activator : MonoBehaviour
{
    [SerializeField] private GameObject _target;
    [SerializeField] private IActivate Target;
    public bool _activated { get; private set; }
    private float DelayTime;
    private Coroutine ActivateCoroutine;

    private void Init()
    {
        if (_target != null && _target.GetComponent<IActivate>() != null)
        {
            Target = _target.GetComponent<IActivate>();
        }
        DelayTime = GameManagement.MainData.DelayTime;
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
        Target.Activate(on);
        _activated = on;
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
