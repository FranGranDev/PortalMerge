﻿using UnityEngine;

[System.Serializable]
public struct SoundItem
{
    [Header("Название(для удобства)")]
    public string name;
    [Header("ID")]
    public string id;
    [Header("Клип")]
    public AudioClip Clip;
    [Header("Громкость")]
    [Range(0, 1)]
    public float Volume;
    [Header("Тон")]
    [Range(0, 2)]
    public float Pitch;
    [Header("Объемность звука(2D/3D)")]
    [Range(0, 1)]
    public float SpatialBlend;
    [Header("Максимальная дальность звука")]
    public float MaxDistance;
}