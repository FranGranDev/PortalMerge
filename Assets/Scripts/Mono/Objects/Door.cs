using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Door : MonoBehaviour, IActivate
{
    private enum CloseOpenType { ButtonCloseOnes, ButtonClose, Timer}
    [SerializeField] private CloseOpenType CloseType;
    [SerializeField] private float OpenedTime;
    [SerializeField] private float ClosedTime;

    [SerializeField] private bool Opened;

    [Header("Components")]
    [SerializeField] private Animator _anim;


    public void Activate(bool on = true)
    {
        switch(CloseType)
        {
            case CloseOpenType.ButtonClose:
                Opened = !on;
                break;
            case CloseOpenType.ButtonCloseOnes:
                Opened = false;
                break;
            case CloseOpenType.Timer:
                Opened = !on;
                break;
        }
        _anim.SetBool("Opened", Opened);
    }

    private IEnumerator CloseOpenCour()
    {
        while(true)
        {
            if(Opened)
            {
                yield return new WaitForSeconds(OpenedTime);
                Activate(true);
            }
            else
            {
                yield return new WaitForSeconds(ClosedTime);
                Activate(false);
            }
            yield return new WaitForFixedUpdate();
        }
    }

    private void Init()
    {
        if (_anim == null) _anim = GetComponent<Animator>();

        if(CloseType == CloseOpenType.Timer)
        {
            StartCoroutine(CloseOpenCour());
        }

        _anim.SetBool("Opened", Opened);
    }

    private void Start()
    {
        Init();
    }
}
