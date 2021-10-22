using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManagment : MonoBehaviour
{
    public static SoundManagment Active { get; private set; }
    public SoundManagment()
    {
        Active = this;
    }

    [SerializeField] private SoundData Data;
    [SerializeField] private GameObject SoundPrefab;

    [SerializeField] private Vector3 ListenerOffset;
    [SerializeField] private GameObject Listener;

    private void DestroySoundOnEnd(AudioSource obj) => StartCoroutine(DestroySoundOnEndCour(obj));
    private IEnumerator DestroySoundOnEndCour(AudioSource obj)
    {
        while(obj.isPlaying)
        {
            yield return new WaitForFixedUpdate();
        }
        Destroy(obj.gameObject);
        yield break;
    }


    public static void PlaySound(string id, Transform obj = null)
    {
        if(Active.Data.Sounds.Exists(item => item.id == id))
        {
            SoundItem ItemData = Active.Data.Sounds.Find(item => item.id == id);
            
            GameObject SoundObject = Instantiate(Active.SoundPrefab, obj);
            SoundObject.name = $"source: {id}";

            AudioSource SoundSource = SoundObject.GetComponent<AudioSource>();
            SoundSource.clip = ItemData.Clip;
            SoundSource.volume = ItemData.Volume;
            SoundSource.pitch = ItemData.Pitch;
            SoundSource.spatialBlend = ItemData.SpatialBlend;

            SoundSource.Play();
            Active.DestroySoundOnEnd(SoundSource);
            
        }
        else
        {
            Debug.Log("Звука с таким Id не найден");
        }
    }

    private void MoveListener()
    {
        Vector3 Position = InputManagement.GetCenterPoint.Point + ListenerOffset;
        Listener.transform.position = Position;
    }

    private void Init()
    {

    }
    private void Awake() => Init();

    private void FixedUpdate()
    {
        MoveListener();
    }
}
