using TMPro;
using UnityEngine;
using DG.Tweening;

public class NumberSelector : MonoBehaviour
{
    [Header("Roller References")]
    [SerializeField] private TMP_Text currentNumberText;
    [SerializeField] private TMP_Text nextNumberText;

    [Header("Roller Settings")]
    [SerializeField] private float rollDistance = 65f;
    [SerializeField] private float rollDuration = 0.25f;

    private int selectedNumber = 1;
    private bool isAnimating = false;

    public int SelectedNumber => selectedNumber;

    private void Start()
    {
        ResetToOne();
    }

    public void IncreaseNumber()
    {
        if (isAnimating) return;

        int nextNumber = selectedNumber >= 7 ? 1 : selectedNumber + 1;

        AnimateNumber(nextNumber, 1);
    }

    public void DecreaseNumber()
    {
        if (isAnimating) return;

        int nextNumber = selectedNumber <= 1 ? 7 : selectedNumber - 1;

        AnimateNumber(nextNumber, -1);
    }

    private void AnimateNumber(int newNumber, int direction)
    {
        isAnimating = true;
        selectedNumber = newNumber;

        currentNumberText.rectTransform.DOKill();
        nextNumberText.rectTransform.DOKill();

        nextNumberText.text = newNumber.ToString();

        Vector2 incomingStart = direction > 0 ? new Vector2(0f, -rollDistance) : new Vector2(0f, rollDistance);
        Vector2 outgoingEnd = -incomingStart;

        nextNumberText.rectTransform.anchoredPosition = incomingStart;

        Sequence roller = DOTween.Sequence();

        roller.Join(currentNumberText.rectTransform.DOAnchorPos(outgoingEnd, rollDuration).SetEase(Ease.InOutCubic));
        roller.Join(nextNumberText.rectTransform.DOAnchorPos(Vector2.zero, rollDuration).SetEase(Ease.InOutCubic));

        roller.OnComplete(() =>
        {
            TMP_Text previousText = currentNumberText;

            currentNumberText = nextNumberText;
            nextNumberText = previousText;

            nextNumberText.rectTransform.anchoredPosition = incomingStart;

            isAnimating = false;
        });
    }

    public void ResetToOne()
    {
        currentNumberText.rectTransform.DOKill();
        nextNumberText.rectTransform.DOKill();

        selectedNumber = 1;
        isAnimating = false;

        currentNumberText.text = "1";
        nextNumberText.text = "1";

        currentNumberText.rectTransform.anchoredPosition = Vector2.zero;

        nextNumberText.rectTransform.anchoredPosition = new Vector2(0f, -rollDistance);
    }
}
