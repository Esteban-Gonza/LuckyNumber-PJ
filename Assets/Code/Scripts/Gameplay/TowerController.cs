using DG.Tweening;
using UnityEngine;

public class TowerController : MonoBehaviour
{
    [Header("Block Settings")]
    [SerializeField] private GameObject blockPrefab;
    [SerializeField] private float blockHeight = 0.4f;
    [SerializeField] private Vector3 blockSize = new Vector3(2f, 0.4f, 1f);
    [SerializeField] private float blockDelay = 0.06f;

    [Header("Balance Settings")]
    [SerializeField] private float maxLeanAngle = 14f;
    [SerializeField] private float maxSway = 2.5f;
    [SerializeField] private float swaySpeed = 3f;

    private int blockCount = 0;
    private float balanceIndex = 0f;

    private bool isFalling = false;
    private int fallDirection;

    public int BlockCount => blockCount;
    public float TowerHeight => blockCount * blockHeight;

    private void Start()
    {
        fallDirection = Random.value < 0.5f ? -1 : 1;
    }

    public void AddBlocks(int amount, Color blockColor)
    {
        if (isFalling) return;

        for (int i = 0; i < amount; i++)
        {
            int currentIndex = blockCount;

            GameObject block = Instantiate(blockPrefab, transform);

            block.transform.localPosition = new Vector3(0f, currentIndex * blockHeight + blockHeight / 2f, 0f);

            Renderer blockRenderer = block.GetComponentInChildren<Renderer>();

            if (blockRenderer != null)
            {
                MaterialPropertyBlock properties =new MaterialPropertyBlock();

                blockRenderer.GetPropertyBlock(properties);

                properties.SetColor("_BaseColor", blockColor);
                properties.SetColor("_Color", blockColor);

                blockRenderer.SetPropertyBlock(properties);
            }

            block.transform.localScale = Vector3.zero;
            block.transform.DOScale(blockSize, 0.2f).SetDelay(i * blockDelay).SetEase(Ease.OutBack);

            blockCount++;
        }
    }

    public void SetBalance(float newBalance)
    {
        if (isFalling) return;

        balanceIndex = Mathf.Clamp(newBalance, 0f, 100f);

        if (balanceIndex >= 100f)
        {
            CollapseTower();
        }
    }

    private void Update()
    {
        if (isFalling) return;

        float normalizedBalance = balanceIndex / 100f;
        float lean = Mathf.Lerp(0f, maxLeanAngle, Mathf.Pow(normalizedBalance, 1.3f));
        float sway = Mathf.Sin(Time.time * swaySpeed) * maxSway * normalizedBalance;
        float finalAngle = fallDirection * lean + sway;

        transform.localRotation = Quaternion.Euler(0f, 0f, finalAngle);
    }

    private void CollapseTower()
    {
        if (isFalling) return;

        isFalling = true;

        Sequence collapse = DOTween.Sequence();

        collapse.Append(transform.DORotate(new Vector3(0f, 0f, fallDirection * 90f), 0.9f).SetEase(Ease.InBack));

        collapse.Join(transform.DOMoveX(transform.position.x + fallDirection * 3f,0.9f).SetEase(Ease.InBack));
    }
}
