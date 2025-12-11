using System;
using Fusion;
using TMPro;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    public enum GameState { Playing, GameOver }
    [Networked] public GameState CurrentState { get; set; }
    [Networked] public NetworkId LoserId { get; set; } 

    private ChangeDetector _changes;
    [SerializeField]private GameObject uiGameOver;

    public void ChangeState(GameState newState)
    {
        CurrentState = newState;
    }

    public override void Spawned()
    {
         _changes = GetChangeDetector(ChangeDetector.Source.SimulationState);
    }
    public override void Render()
    {
        foreach(var change in _changes.DetectChanges(this, out var previousBuffer, out var currentBuffer))
            {
                switch (change)
                {
                    case nameof(CurrentState):
                        var reader = GetPropertyReader<GameState>(nameof(CurrentState));
                        var (previous,current) = reader.Read(previousBuffer, currentBuffer);
                        Debug.Log($"CurrentState changed from {previous} to {current}");
                        if(current == GameState.GameOver)
                        {
                            GameOver();
                        }
                        break;

                }
            }
    }

    public void TriggerGameOver(NetworkId loserId)
    {
        LoserId = loserId;
        ChangeState(GameState.GameOver);
    }


    [SerializeField]private TMP_Text finaltext;
    public void GameOver()
    {
        uiGameOver.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        var players = FindObjectsByType<Player>(FindObjectsSortMode.None);
        foreach (var player in players)
        {
            if (player.HasInputAuthority)
            {
                player.InputsAllowed = false;
                if (player.Object.Id != LoserId)
                {
                    Debug.Log("YOU WIN!");
                    finaltext.text = "You Win!";
                }
                else
                {
                    Debug.Log("YOU LOSE!");
                    finaltext.text = "You Lose!";
                }
                
                break;
            }
        }
    }

    public void MainMenu()
    {
        if(Runner != null)
        {
            Runner.Shutdown();
        }
        
    }
}
