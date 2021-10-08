using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [Header("Components")]
    private Animator _animator;
    private const string ANIM_ID = "State";

    public enum UIState {InGame, Start, Failed, Done};
    private UIState CurrantState;
    private void SetState(UIState state)
    {
        CurrantState = state;

        _animator.SetInteger(ANIM_ID, (int)CurrantState);
    }

    public void Play()
    {
        LevelManagement.Default.StartGame();
    }
    public void Restart()
    {
        LevelManagement.Default.RestartLevel();
    }
    public void Next()
    {
        LevelManagement.Default.NextLevel();
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

    private void Init()
    {
        _animator = GetComponent<Animator>();
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
