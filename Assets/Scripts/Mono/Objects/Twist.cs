using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Twist : MonoBehaviour, IActivate
{
    [SerializeField] private float RotationSpeed;
    private float CurrantRotationSpeed;
    [SerializeField] private float Force;
    [SerializeField] private bool isActive;
    [SerializeField] private Transform Center;

    private ISound Sound;

    public float Impulse()
    {
        return CurrantRotationSpeed / Mathf.Abs(RotationSpeed) * Force;
    }

    public void Activate(bool on = true)
    {
        isActive = on;
    }

    private void FixedUpdate()
    {
        CurrantRotationSpeed = Mathf.Lerp(CurrantRotationSpeed, RotationSpeed * (isActive ? 1 : 0), GameManagement.MainData.ObstacleAcceleration * 0.1f);
        Center.Rotate(Vector3.up, CurrantRotationSpeed * Time.fixedDeltaTime);

        if(Sound != null)
        {
            Sound.ChangeVolume(CurrantRotationSpeed / RotationSpeed);
        }
    }
    public void ReduseSpeed(float Procent)
    {
        CurrantRotationSpeed *= (1 - Procent);
    }

    private void Awake() => Init();

    private void Init()
    {
        TryGetComponent(out Sound);
        if(Sound != null)
        {
            Sound.Init(isActive);
        }
    }



}
