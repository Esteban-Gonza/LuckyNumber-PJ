
using UnityEngine;
using TMPro;
using DG.Tweening;

public class GameManager : MonoBehaviour
{
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
    private bool gameOver = false;

    private void Start()
    {
        playerCount = Mathf.Clamp(playerCount, 1, 4);

        resultText.text = "";

        BeginTurn();
        UpdateHeight();
    }

    private void BeginTurn()
    {
        if (gameOver) return;

        numberSelector.ResetToOne();

        luckyNumber = Random.Range(1, 8);

        do
        {
            unluckyNumber = Random.Range(1, 8);
        }
        while (unluckyNumber == luckyNumber);

        turnLocked = false;

        turnText.text = playerCount == 1 ? "YOUR TURN" : "PLAYER " + currentPlayer + "'S TURN";
        resultText.text = "Choose your blocks!";

        turnNumber++;

        Debug.Log($"Turno {turnNumber} | " + $"Jugador {currentPlayer + 1} | " + $"Suerte: {luckyNumber} | " + $"Mala suerte: {unluckyNumber}");
    }

    public void ConfirmSelection()
    {
        if (turnLocked || gameOver) return;

        turnLocked = true;

        float previousBalance = balanceIndex;
        int selected = numberSelector.SelectedNumber;

        int blocksToAdd = selected;

        Material resultMaterial;

        if (selected == luckyNumber)
        {
            int decrease = Random.Range(luckyDecreaseRange.x, luckyDecreaseRange.y + 1);

            balanceIndex -= decrease;

            resultMaterial = luckyMaterial;

            resultText.text = "LUCKY! The tower steadies.";
        }
        else if (selected == unluckyNumber)
        {
            int increase = Random.Range(unluckyIncreaseRange.x, unluckyIncreaseRange.y + 1);

            balanceIndex += increase;

            resultMaterial = unluckyMaterial;

            resultText.text = "BAD LUCK! The tower shakes!";
        }
        else
        {
            int increase = Random.Range(neutralIncreaseRange.x, neutralIncreaseRange.y + 1);

            balanceIndex += increase;

            resultMaterial = neutralMaterial;

            resultText.text = "NEUTRAL! The tower wobbles.";
        }

        balanceIndex = Mathf.Clamp(balanceIndex,0f,100f);
        Debug.Log($"BALANCE UPDATE | " + $"Balance anterior: {previousBalance}% | " + $"Balance actual: {balanceIndex}% | " +);

        tower.AddBlocks(blocksToAdd, resultMaterial);
        tower.SetBalance(balanceIndex);

        UpdateHeight();

        if (balanceIndex >= 100f)
        {
            EndGame();
            return;
        }

        DOVirtual.DelayedCall(resultDisplayDuration, NextTurn);
    }

    private void NextTurn()
    {
        if (gameOver) return;

        currentPlayer++;

        if (currentPlayer > playerCount)
        {
            currentPlayer = 1;
        }

        BeginTurn();
    }

    private void UpdateHeight()
    {
        heightText.text = "HEIGHT: " + tower.BlockCount;
    }

    private void EndGame()
    {
        gameOver = true;
        turnLocked = true;

        turnText.text = "GAME OVER";

        resultText.text = playerCount == 1 ? "The tower collapsed!" : "PLAYER " + currentPlayer + " LOST!";

        Debug.Log("Game Over. Final height: " + tower.BlockCount);
    }
}
