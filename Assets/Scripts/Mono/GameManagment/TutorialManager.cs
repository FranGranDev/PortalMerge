using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int TutorNum;

    private void OnGameStart()
    {
        StartTutor();
    }
    private void OnCubeTaken()
    {
        EndTutor();
    }

    private void StartTutor()
    {
        UIManager.Active.TurnTutor(TutorNum);
    }
    private void EndTutor()
    {
        UIManager.Active.TurnTutor(-1);
    }

    private void Start()
    {
        GameManagement.OnGameStarted += OnGameStart;
        InputManagement.Active.OnTakeCube += OnCubeTaken;
    }
    private void OnDisable()
    {
        GameManagement.OnGameStarted -= OnGameStart;
        InputManagement.Active.OnTakeCube -= OnCubeTaken;
    }
}
