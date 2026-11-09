using UnityEngine;
using TMPro;
using DG.Tweening;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    private enum GameState
    {
        Initializing,
        AwaitingSelection,
        ResolvingTurn,
        GameOver
    }

    [Header("Game Settings")]
    [Range(1, 4)]
    [SerializeField] private int playerCount = 2;

    [Header("Balance Configuration")]
    [SerializeField] private Vector2Int luckyDecreaseRange = new Vector2Int(8, 15);
    [SerializeField] private Vector2Int neutralIncreaseRange = new Vector2Int(2, 4);
    [SerializeField] private Vector2Int unluckyIncreaseRange = new Vector2Int(12, 25);

    [Header("Block Materials")]
    [SerializeField] private Material luckyMaterial;
    [SerializeField] private Material neutralMaterial;
    [SerializeField] private Material unluckyMaterial;

    [Header("References")]
    [SerializeField] private NumberSelector numberSelector;
    [SerializeField] private TowerController tower;
    [SerializeField] Button[] SelectorBtns;

    [Space(5)][Header("UI")]
    [SerializeField] private TMP_Text turnText;
    [SerializeField] private TMP_Text heightText;
    [SerializeField] private TMP_Text resultText;

    [Header("Feedback Settings")]
    [SerializeField] private float resultDisplayDuration = 2.5f;

    private int currentPlayer = 1;
    private int turnNumber = 0;

    private int luckyNumber;
    private int unluckyNumber;

    private float balanceIndex = 0f;

    private bool turnLocked = false;

    private GameState currentState = GameState.Initializing;
    private Tween turnTransitionTween;

    private void Start()
    {
        playerCount = Mathf.Clamp(playerCount, 1, 4);

        if (!ValidateReferences())
        {
            enabled = false;
            return;
        }

        resultText.text = "";
        UpdateHeight();
        BeginTurn();
    }

    private bool ValidateReferences()
    {
        if (numberSelector == null || tower == null ||turnText == null || heightText == null || resultText == null)
        {
            Debug.LogError("References missing on GameManager. ");

            return false;
        }

        if (luckyMaterial == null || neutralMaterial == null || unluckyMaterial == null)
        {
            Debug.LogError("Material not assigned");

            return false;
        }

        return true;
    }

    private void BeginTurn()
    {
        if (currentState == GameState.GameOver)
            return;

        currentState = GameState.AwaitingSelection;

        numberSelector.ResetToOne();

        for (int i = 0; i < SelectorBtns.Length; i++)
        {
            SelectorBtns[i].interactable = true;
        }

        luckyNumber = Random.Range(1, 8);

        do
        {
            unluckyNumber = Random.Range(1, 8);
        }
        while (unluckyNumber == luckyNumber);

        turnLocked = false;
        turnNumber++;

        turnText.text = playerCount == 1 ? "YOUR TURN" : "PLAYER " + currentPlayer + "'S TURN";

        resultText.color = new Color32(0xE6, 0xB6, 0x5C, 0xFF);
        resultText.text = "Choose your blocks!";

        Debug.Log($"TURN {turnNumber} | " + $"Player: {currentPlayer} | " + $"Luck: {luckyNumber} | " + $"Bad Luck: {unluckyNumber}");
    }

    public void ConfirmSelection()
    {
        if (currentState != GameState.AwaitingSelection || turnLocked || numberSelector.IsAnimating)
            return;

        turnLocked = true;
        currentState = GameState.ResolvingTurn;

        for (int i = 0; i < SelectorBtns.Length; i++)
        {
            SelectorBtns[i].interactable = false;
        }

        int selected = numberSelector.SelectedNumber;
        int blocksToAdd = selected;

        float previousBalance = balanceIndex;

        Material resultMaterial;
        string resultName;

        if (selected == luckyNumber)
        {
            int decrease = Random.Range(luckyDecreaseRange.x, luckyDecreaseRange.y + 1);

            balanceIndex -= decrease;

            resultMaterial = luckyMaterial;
            resultName = "LUCKY";

            resultText.color = new Color32(0x38, 0xA8, 0x78, 0xFF);
            resultText.text = "LUCKY! The tower steadies.";
        }
        else if (selected == unluckyNumber)
        {
            int increase = Random.Range(unluckyIncreaseRange.x,unluckyIncreaseRange.y + 1);

            balanceIndex += increase;

            resultMaterial = unluckyMaterial;
            resultName = "UNLUCKY";

            resultText.color = new Color32(0xE4, 0x5C, 0x59, 0xFF);
            resultText.text = "BAD LUCK! The tower shakes!";
        }
        else
        {
            int increase = Random.Range(neutralIncreaseRange.x, neutralIncreaseRange.y + 1);

            balanceIndex += increase;

            resultMaterial = neutralMaterial;
            resultName = "NEUTRAL";

            resultText.color = new Color32(0xE6, 0xB6, 0x5C, 0xFF);
            resultText.text = "NEUTRAL! The tower wobbles.";
        }

        balanceIndex = Mathf.Clamp(balanceIndex, 0f, 100f);

        float balanceVariation = balanceIndex - previousBalance;

        Debug.Log($"RESULT | " +$"Turn: {turnNumber} | " + $"Player: {currentPlayer} | " + $"Selected number: {selected} | " + $"Result: {resultName}");
        Debug.Log($"BALANCE UPDATE | " + $"Previous: {previousBalance:F0}% | " + $"Current: {balanceIndex:F0}% | " + $"Variation: {balanceVariation:+0;-0;0}%");

        tower.AddBlocks(blocksToAdd, resultMaterial);

        UpdateHeight();

        if (balanceIndex >= 100f)
        {
            resultText.color = new Color32(0xE4, 0x5C, 0x59, 0xFF);
            resultText.text = "THE TOWER IS COLLAPSING!";
            turnText.text = "HOLD ON!";

            tower.SetBalance(balanceIndex, EndGame);

            return;
        }

        tower.SetBalance(balanceIndex);

        turnTransitionTween?.Kill();
        turnTransitionTween = DOVirtual.DelayedCall(resultDisplayDuration, NextTurn);
    }

    private void NextTurn()
    {
        if (currentState == GameState.GameOver)
            return;

        currentPlayer++;

        if (currentPlayer > playerCount)
            currentPlayer = 1;

        BeginTurn();
    }

    private void UpdateHeight()
    {
        heightText.text = "HEIGHT: " + tower.BlockCount;
    }

    private void EndGame()
    {
        currentState = GameState.GameOver;
        turnLocked = true;

        for (int i = 0; i < SelectorBtns.Length; i++)
        {
            SelectorBtns[i].interactable = false;
        }

        turnTransitionTween?.Kill();
        turnTransitionTween = null;

        turnText.text = "GAME OVER";

        resultText.color = new Color32(0xE4, 0x5C, 0x59, 0xFF);
        resultText.text = playerCount == 1 ? "The tower collapsed!" : "PLAYER " + currentPlayer + " LOST!";

        Debug.Log($"GAME OVER | " + $"Loser player: {currentPlayer} | " + $"Final Height: {tower.BlockCount} | " + $"Final balance: {balanceIndex:F0}%");
    }

    private void OnDisable()
    {
        turnTransitionTween?.Kill();
        turnTransitionTween = null;
    }
}