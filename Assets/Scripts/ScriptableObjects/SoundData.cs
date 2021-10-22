using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SoundData", menuName = "Sounds")]
public class SoundData : ScriptableObject
{
    [SerializeField] private List<SoundItem> soundItems = new List<SoundItem>();

    public List<SoundItem> Sounds { get { return soundItems; } }
}