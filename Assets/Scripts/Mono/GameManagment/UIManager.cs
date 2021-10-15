using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Active { get; private set; }
    private const string LEVEL_NAME = "Level";
    private const string GEM_COLLECTED = "GemCollected";
    private int GemCollected;

    [Header("Text")]
    [SerializeField] private TextMeshProUGUI StartLevelNum;
    [SerializeField] private TextMeshProUGUI GemNum;
    [Header("Components")]
    [SerializeField] private RectTransform GemIcon;
    private Animator _animator;
    
    private const string ANIM_ID = "State";

    public enum UIState {InGame, Start, Failed, Done};
    [SerializeField] private UIState CurrantState = UIState.Start;
    private void SetState(UIState state)
    {
        CurrantState = state;

        _animator.SetInteger(ANIM_ID, (int)CurrantState);

        if(state == UIState.Start)
        {
            SetLevelText();
        }
    }

    public void Play()
    {
        if (CurrantState != UIState.Start)
            return;
        LevelManagement.Default.StartGame();
    }
    public void Restart()
    {
        if (CurrantState != UIState.Failed)
            return;
        LevelManagement.Default.RestartLevel();

        SetState(UIState.Start);
    }
    public void Next()
    {
        if (CurrantState != UIState.Done)
            return;
        LevelManagement.Default.NextLevel();

        SetState(UIState.Start);
    }

    private void OnGameStarted()
    {
        SetState(UIState.InGame);
    }
    private void OnGameFailed()
    {
        SetState(UIState.Failed);
    }
    private void OnGameDone()
    {
        SetState(UIState.Done);
    }

    public void OnGemCollected(ICollected gem)
    {
        Vector2 Pos = Camera.main.WorldToScreenPoint(gem.GemTransform.position);
        GemCollected++;
        SetGemNum();
        StartCoroutine(GemFly(Pos));
    }
    private void SetGemNum()
    {
        GemNum.text = GemCollected.ToString();
    }
    private IEnumerator GemFly(Vector3 StartPos)
    {
        GameObject gem = Instantiate(GameManagement.MainData.GemIcon, StartPos, Quaternion.identity, transform);

        gem.transform.SetAsFirstSibling();
        yield return new WaitForSeconds(GameManagement.MainData.GemDelayToFly);
        while(((Vector2)gem.transform.position - (Vector2)GemIcon.transform.position).magnitude > 1f)
        {
            float Speed = 1 / Mathf.Sqrt(((Vector2)gem.transform.position - (Vector2)GemIcon.transform.position).magnitude);
            gem.transform.position = Vector2.Lerp(gem.transform.position, GemIcon.transform.position, GameManagement.MainData.GemFlySpeed * (0.1f + Speed));
            yield return new WaitForFixedUpdate();
        }
        _animator.Play(GEM_COLLECTED, 1, 0);
        Destroy(gem);
        yield break;
    }

    private void SubscribeForAction()
    {
        GameManagement.OnGameFailed += OnGameFailed;
        GameManagement.OnGameWin += OnGameDone;
        GameManagement.OnGameStarted += OnGameStarted;
    }
    private void UnsubscribeForAction()
    {
        GameManagement.OnGameFailed -= OnGameFailed;
        GameManagement.OnGameWin -= OnGameDone;
        GameManagement.OnGameStarted -= OnGameStarted;
    }

    private void SetLevelText()
    {
        if (StartLevelNum == null)
            return;
        StartLevelNum.text = LEVEL_NAME + " " + (LevelManagement.Default.CurrentLevelIndex + 1);
    }

    private void Init()
    {
        Active = this;
        _animator = GetComponent<Animator>();

        SetLevelText();
        SetGemNum();
    }

    private void Awake()
    {
        Init();
    }
    private void Start()
    {
        SubscribeForAction();
    }
    private void OnDestroy()
    {
        UnsubscribeForAction();
    }
}
