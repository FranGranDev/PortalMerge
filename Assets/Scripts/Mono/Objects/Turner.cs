using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class Turner : MonoBehaviour, IInteract
{
    [Header("Settings")]
    public float RotationTime;
    [SerializeField] private Transform Rotation1;
    [SerializeField] private Transform Rotation2;
    [SerializeField] private Ease Ease = Ease.InOutSine;
    public bool Turned { get; private set; }
    private bool ClockRotation;
    private bool isRotating;

    [Header("Components")]
    [SerializeField] private Rigidbody Main;
    [SerializeField] private GameObject Circle1;
    [SerializeField] private GameObject Circle2;

    public void DoAction()
    {
        if (isRotating)
            return;
        if(Turned)
        {
            RotateToStart();
        }
        else
        {
            RotateToEnd();
        }
    }

    private void RotateToEnd()
    {
        Main
        .DORotate(Rotation2.rotation.eulerAngles, RotationTime)
        .SetEase(Ease)
        .OnComplete(() => {
            OnRotatingEnd();
        });
        isRotating = true;
    }
    private void RotateToStart()
    {
        Main
        .DORotate(Rotation1.rotation.eulerAngles, RotationTime)
        .SetEase(Ease)
        .OnComplete(() => {
            OnRotatingEnd();
        });
        isRotating = true;
    }
    private void OnRotatingEnd()
    {
        Turned = !Turned;
        isRotating = false;

        StartCoroutine(ChangeClockCour());
    }
    private IEnumerator ChangeClockCour()
    {
        Circle1.SetActive(ClockRotation ? Turned : !Turned);
        Circle2.SetActive(ClockRotation ? !Turned : Turned);
        Circle1.transform.localScale = Vector3.zero;
        Circle2.transform.localScale = Vector3.zero;
        if(Circle1.activeSelf)
        {
            Circle1.transform.localScale = Vector3.one;
            while (Circle1.transform.localScale.magnitude > 0.05f)
            {
                Circle1.transform.localScale = Vector3.Lerp(Circle1.transform.localScale, Vector3.zero, 0.1f);
                yield return new WaitForFixedUpdate();
            }
            yield return new WaitForSeconds(0.1f);
            Circle2.transform.localScale = Vector3.zero;
            while (Circle2.transform.localScale.magnitude < 0.95f)
            {
                Circle2.transform.localScale = Vector3.Lerp(Circle2.transform.localScale, Vector3.one, 0.1f);
                yield return new WaitForFixedUpdate();
            }
        }
        else
        {
            Circle2.transform.localScale = Vector3.one;
            while (Circle2.transform.localScale.magnitude > 0.05f)
            {
                Circle2.transform.localScale = Vector3.Lerp(Circle2.transform.localScale, Vector3.zero, 0.1f);
                yield return new WaitForFixedUpdate();
            }
            yield return new WaitForSeconds(0.1f);
            Circle1.transform.localScale = Vector3.zero;
            while (Circle1.transform.localScale.magnitude < 0.95f)
            {
                Circle1.transform.localScale = Vector3.Lerp(Circle1.transform.localScale, Vector3.one, 0.1f);
                yield return new WaitForFixedUpdate();
            }
        }
        if (ClockRotation ? Turned : !Turned)
        {
            
        }
        else
        {
            
            
            
        }
        yield break;
    }

    private void Init()
    {
        ClockRotation = Rotation2.localRotation.y < Rotation1.localRotation.y;
        Debug.Log(ClockRotation);
        Circle1.SetActive(!ClockRotation);
        Circle2.SetActive(ClockRotation);
    }

    private void Awake()
    {

        Init();
    }
}
