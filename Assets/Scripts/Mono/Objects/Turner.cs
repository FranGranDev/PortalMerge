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
        Circle1.SetActive(ClockRotation ? Turned : !Turned);
        Circle2.SetActive(ClockRotation ? !Turned : Turned);
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
