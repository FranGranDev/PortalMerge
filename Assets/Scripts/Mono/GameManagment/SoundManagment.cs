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
    [SerializeField] private AudioSource MusicSource;
    private float MusicVol;

    private void DestroySoundOnEnd(AudioSource obj, float Delay) => StartCoroutine(DestroySoundOnEndCour(obj, Delay));
    private IEnumerator DestroySoundOnEndCour(AudioSource obj, float Delay)
    {
        yield return new WaitForSeconds(Delay);
        while(obj != null && obj.isPlaying)
        {
            yield return new WaitForFixedUpdate();
        }
        if (obj != null)
        {
            Destroy(obj.gameObject);
        }
        yield break;
    }

    public static void PlayMusic()
    {
        Active.MusicVol = Active.MusicSource.volume;
        Active.MusicSource.Play();
    }
    private void CheckMuteMusic()
    {
        MusicSource.volume = MusicVol * (GameManagement.MainData.MuteMusic ? 0 : 1);
    }
    public static void PlaySound(string id, Transform obj = null, float Delay = 0)
    {
        if (GameManagement.MainData.MuteEffect)
            return;
        if(Active.Data.Sounds.Exists(item => item.id == id))
        {
            SoundItem ItemData = Active.Data.Sounds.Find(item => item.id == id);
            
            GameObject SoundObject = Instantiate(Active.SoundPrefab, GameManagement.Active.LevelTransform);
            if (obj != null)
            {
                SoundObject.transform.position = obj.position;
            }
            SoundObject.name = $"source: {id}";

            AudioSource SoundSource = SoundObject.GetComponent<AudioSource>();
            SoundSource.clip = ItemData.Clip;
            SoundSource.volume = ItemData.Volume;
            SoundSource.pitch = ItemData.Pitch;
            SoundSource.spatialBlend = ItemData.SpatialBlend;
            SoundSource.maxDistance = ItemData.MaxDistance;

            SoundSource.PlayDelayed(Delay);
            Active.DestroySoundOnEnd(SoundSource, Delay);
            
        }
        else
        {
            Debug.Log("Звука с таким Id не найден");
        }
    }

    private void MoveListener()
    {
        Vector3 Position = InputManagement.GetListenerPoint() + ListenerOffset;
        Listener.transform.position = Vector3.Lerp(Listener.transform.position, Position, 0.1f);
    }

    private void Init()
    {

    }
    private void Awake() => Init();
    private void Start()
    {
        PlayMusic();
    }

    private void FixedUpdate()
    {
        MoveListener();
        CheckMuteMusic();
    }
}
