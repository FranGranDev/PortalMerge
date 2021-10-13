using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Active { get; private set; }
    private const string LEVEL_NAME = "Level";

    [Header("Text")]
    [SerializeField] private TextMeshProUGUI StartLevelNum;
    [Header("Components")]
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
