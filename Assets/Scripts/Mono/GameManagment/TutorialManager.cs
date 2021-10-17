using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialManager : MonoBehaviour
{
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
        UIManager.Active.TurnTutor(true);
    }
    private void EndTutor()
    {
        UIManager.Active.TurnTutor(false);
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
