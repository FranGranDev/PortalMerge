﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Laser : MonoBehaviour, IActivate
{
    [SerializeField] private bool isActive;
    [SerializeField] private float DestroyDelay;
    private ICube prevCube;
    private MeshRenderer _renderer;


    public void Activate(bool on = true)
    {
        isActive = on;

        _renderer.enabled = on;
    }

    private void OnCubeEntered(ICube cube)
    {
        if(isActive && cube != prevCube)
        {
            prevCube = cube;
            StartCoroutine(DestroyCube(cube));
        }
    }
    private IEnumerator DestroyCube(ICube cube)
    {
        yield return new WaitForSeconds(DestroyDelay);
        cube.DestroyCube();
        yield break;
    }

    private void Init()
    {
        _renderer = GetComponent<MeshRenderer>();
        DestroyDelay = GameManagement.MainData.DestroyDelay;
        Activate(isActive);
    }
    private void Start()
    {
        Init();
    }

    private void OnTriggerStay(Collider other)
    {
        if(other.tag == "Cube")
        {
            ICube cube = other.GetComponent<ICube>();
            OnCubeEntered(cube);
        }
    }
}