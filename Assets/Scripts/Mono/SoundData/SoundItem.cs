using UnityEngine;

[System.Serializable]
public struct SoundItem
{
    [Header("ID")]
    public string id;
    [Header("Клип")]
    public AudioClip Clip;
    [Header("Громкость")]
    [Range(0, 1)]
    public float Volume;
    [Header("Тон")]
    [Range(0, 1)]
    public float Pitch;
    [Header("Объемность звука")]
    [Range(0, 1)]
    public float SpatialBlend;
}