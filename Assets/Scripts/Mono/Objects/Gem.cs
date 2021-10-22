using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gem : MonoBehaviour, ICollected
{
    [Range(0, 1f)]
    [SerializeField] private float MoveOffset;
    [Range(0, 1f)]
    [SerializeField] private float MoveSpeed;
    [Range(0, 1f)]
    [SerializeField] private float RotationSpeed;
    private Vector3 StartPosition;
    private bool MoveUp;

    public delegate void OnColldected(ICollected gem);
    public OnColldected OnGemColldected;

    public Transform GemTransform { get => transform; }

    public void SubscribeFor(OnColldected OnCollected, bool unsubscribeFor = false)
    {
        if(unsubscribeFor)
        {
            OnGemColldected -= OnCollected;
        }
        else
        {
            OnGemColldected += OnCollected;
        }
        
    }

    private void OnCollected()
    {
        OnGemColldected?.Invoke(this);

        CreateCollectedParticle();

        SoundManagment.PlaySound("gem_take", transform);

        Destroy(gameObject);
    }
    private void CreateCollectedParticle()
    {
        ParticleSystem partilce = Instantiate(GameManagement.MainData.GemCollected, transform.position, transform.rotation);
        partilce.transform.localScale = transform.localScale;
    }

    private IEnumerator AnimationCour()
    {
        while(true)
        {
            transform.Rotate(Vector3.forward, RotationSpeed);
            if(MoveUp)
            {
                Vector3 Offset = transform.position + Vector3.up * MoveOffset;
                transform.position = Vector3.Lerp(transform.position, Offset, MoveSpeed / 10);
                if (transform.position.y > StartPosition.y + MoveOffset)
                    MoveUp = false;
                yield return new WaitForFixedUpdate();
            }
            else
            {
                Vector3 Offset = transform.position - Vector3.up * MoveOffset;
                transform.position = Vector3.Lerp(transform.position, Offset, MoveSpeed / 10);
                if (transform.position.y < StartPosition.y - MoveOffset)
                    MoveUp = true;
                yield return new WaitForFixedUpdate();
            }
        }
    }

    public void Init()
    {
        OnGemColldected = null;

        StartPosition = transform.position;
        StartCoroutine(AnimationCour());
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.tag == "Cube")
        {
            OnCollected();
        }
    }
}
public interface ICollected
{
    Transform GemTransform { get; }

    void Init();

    void SubscribeFor(Gem.OnColldected OnCollected, bool unsubscribeFor = false);
}