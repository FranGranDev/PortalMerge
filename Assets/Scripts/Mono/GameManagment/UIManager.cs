using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Active { get; private set; }
    private const string LEVEL_NAME = "LEVEL";
    private const string LEVEL_FAILED = "FAILED";
    private const string LEVEL_DONE = "DONE";
    private const string GEM_COLLECTED = "GemCollected";
    private const string ANIM_ID = "State";
    private const string ANIM_COMBO = "Combo";
    private const string ANIM_TUTOR = "TutorNum";
    private const string ANIM_CURSORSHOW = "ShowCursor";
    private const string ANIM_CURSORCLICK = "ClickCursor";
    private int GemCollected;
    private int GemCollectedInRow;

    [Header("Text")]
    [SerializeField] private TextMeshProUGUI StartLevelNum;
    [SerializeField] private TextMeshProUGUI DoneLevelNum;
    [SerializeField] private TextMeshProUGUI FailedLevelNum;
    [SerializeField] private TextMeshProUGUI GemNum;
    [SerializeField] private TextMeshProUGUI PlusNum;
    [Header("Components")]
    [SerializeField] private RectTransform GemIcon;
    [SerializeField] private RectTransform PlusIcon;
    [SerializeField] private RectTransform Cursor;
    [SerializeField] private RectTransform CursorHand;
    private Animator _animator;

    private Coroutine ShowComboCoroutine;

    public enum UIState {InGame, Start, Failed, Done};
    [SerializeField] private UIState CurrantState = UIState.Start;
    private void SetState(UIState state)
    {
        CurrantState = state;

        _animator.SetInteger(ANIM_ID, (int)CurrantState);
    }

    public void Play()
    {
        if (CurrantState != UIState.Start)
            return;
        LevelManagement.Default.StartGame();

        SoundManagment.PlaySound("ui_button");
    }
    public void Restart()
    {
        if (CurrantState != UIState.Failed)
            return;
        LevelManagement.Default.RestartLevel();

        SetState(UIState.Start);
        SoundManagment.PlaySound("ui_button");
    }
    public void GameRestart()
    {
        if (CurrantState != UIState.InGame)
            return;
        LevelManagement.Default.RestartLevel();
        GameManagement.isGameStarted = false;
        TurnTutor(-1);
        SetState(UIState.Start);
        SoundManagment.PlaySound("ui_button");
    }
    public void Next()
    {
        if (CurrantState != UIState.Done)
            return;
        LevelManagement.Default.NextLevel();

        SetState(UIState.Start);
        SoundManagment.PlaySound("ui_button");
    }

    public void TurnTutor(int num)
    {
        _animator.SetInteger(ANIM_TUTOR, num);
    }

    private void OnTakeCube()
    {
        _animator.SetBool(ANIM_CURSORSHOW, true);
    }
    private void OnThrowCube()
    {
        _animator.SetBool(ANIM_CURSORSHOW, false);
    }
    private void OnInteract()
    {
        _animator.SetTrigger(ANIM_CURSORCLICK);
    }
    private void SetCursorPosition()
    {
        if(GameManagement.MainData.ShowHandCursor)
        {
            Cursor.position = InputManagement.GetCursorPoint();
        }
    }

    private void OnGameStarted()
    {
        SetState(UIState.InGame);
    }
    private void OnGameFailed()
    {
        OnThrowCube();

        SetState(UIState.Failed);
    }
    private void OnGameDone()
    {
        OnThrowCube();

        SetState(UIState.Done);
    }

    public void OnGemCollected(ICollected gem)
    {
        Vector2 Pos = Camera.main.WorldToScreenPoint(gem.GemTransform.position);
        GemCollected++;
        SetGemNum();
        StartCoroutine(GemFly(Pos));
        if(ShowComboCoroutine != null)
        {
            StopCoroutine(ShowComboCoroutine);
        }
        ShowComboCoroutine = StartCoroutine(ShowComboCour(Pos));
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
    private IEnumerator ShowComboCour(Vector3 StartPos)
    {
        GemCollectedInRow++;
        if(StartPos.x > Screen.width * 0.5f)
        {
            StartPos += Vector3.right * -50;
        }
        else if(StartPos.x < Screen.width * 0.5f)
        {
            StartPos += Vector3.right * 50;
        }
        PlusIcon.transform.position = StartPos;
        _animator.Play(ANIM_COMBO, 2, 0);
        PlusNum.text = "+" + GemCollectedInRow.ToString();
        yield return new WaitForSeconds(1f);
        GemCollectedInRow = 0;
        yield break;
    }

    private void SubscribeForAction()
    {
        GameManagement.OnGameFailed += OnGameFailed;
        GameManagement.OnGameWin += OnGameDone;
        GameManagement.OnGameStarted += OnGameStarted;
        if(GameManagement.MainData.ShowHandCursor)
        {
            InputManagement.Active.SubscibeForInteract(OnInteract);
            InputManagement.Active.SubscibeForLostInput(OnThrowCube);
            InputManagement.Active.SubscibeForTakeCube(OnTakeCube);
        }
    }
    private void UnsubscribeForAction()
    {
        GameManagement.OnGameFailed -= OnGameFailed;
        GameManagement.OnGameWin -= OnGameDone;
        GameManagement.OnGameStarted -= OnGameStarted;
        if (GameManagement.MainData.ShowHandCursor)
        {
            InputManagement.Active.SubscibeForInteract(OnInteract, true);
            InputManagement.Active.SubscibeForLostInput(OnThrowCube, true);
            InputManagement.Active.SubscibeForTakeCube(OnTakeCube, true);
        }
    }



    public void SetLevelText() //Calls from NULL_State in animations
    {
        StartLevelNum.text = LEVEL_NAME + " " + (LevelManagement.Default.CurrentLevelIndex + 1);
        DoneLevelNum.text = LEVEL_NAME + " " + (LevelManagement.Default.CurrentLevelIndex + 1) + " " + LEVEL_DONE;
        FailedLevelNum.text = LEVEL_NAME + " " + (LevelManagement.Default.CurrentLevelIndex + 1) + " " + LEVEL_FAILED;
    }

    private void Init()
    {
        Active = this;
        _animator = GetComponent<Animator>();

        SetLevelText();
        SetGemNum();
        GemCollectedInRow = 0;

        TurnTutor(-1);
    }

    private void FixedUpdate()
    {
        SetCursorPosition();
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
