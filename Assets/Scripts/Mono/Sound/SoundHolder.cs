using UnityEngine;

public class SoundHolder : MonoBehaviour, ISound
{

    private AudioSource _source;

    public float CurrantVolume { get; private set; }
    public float StartVolume { get; private set; }

    public void ChangeVolume(float Ratio)
    {
        CurrantVolume = StartVolume * (Mathf.Abs(Ratio) > 1 ? 1 : Mathf.Abs(Ratio));
    }
    public void Mute(bool on)
    {
        if(on)
        {
            CurrantVolume = 0;
        }
        else
        {
            CurrantVolume = StartVolume;
        }
    }

    public void Init(bool PlayOnAwake)
    {
        _source = GetComponent<AudioSource>();
        StartVolume = _source.volume;


        if (!PlayOnAwake)
        {
            CurrantVolume = 0;
            _source.volume = CurrantVolume;
        }
        else
        {
            CurrantVolume = StartVolume;
            _source.volume = CurrantVolume;
        }

        _source.Play();
    }

    private void FixedUpdate() => SoundExecute();
    

    private void SoundExecute()
    {
        _source.volume = Mathf.Lerp(_source.volume, CurrantVolume, 0.1f);
    }
}

public interface ISound
{
    float CurrantVolume { get; }
    float StartVolume { get; }
    void ChangeVolume(float Ratio);
    void Mute(bool on);

    void Init(bool PlayOnAwake);
}
