using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Turrel : MonoBehaviour, IActivate
{
    [SerializeField] private bool Activated;

    public void Activate(bool on)
    {
        Activated = on;
    }
}
