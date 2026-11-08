
using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class TowerController : MonoBehaviour
{
    private class BlockData
    {
        public GameObject gameObject;
        public Transform blockTransform;
        public Vector3 originalPosition;
        public Quaternion originalRotation;
    }

    [Header("Block Settings")]
    [SerializeField] private GameObject blockPrefab;
    [SerializeField] private float blockHeight = 0.4f;
    [SerializeField] private Vector3 blockSize = new Vector3(2f, 0.4f, 1f);
    [SerializeField] private float blockDelay = 0.06f;

    [Header("Progressive Tower Sway")]
    [SerializeField] private float maxLeanAngle = 5f;
    [SerializeField] private float maxTopOffset = 0.45f;
    [SerializeField] private float maxTopSway = 0.08f;
    [SerializeField] private float maxSway = 1f;
    [SerializeField] private float swaySpeed = 2f;
    [SerializeField] private float leanExponent = 1.5f;

    [Header("Collapse Settings")]
    [SerializeField] private float collapseStartDelay = 0.65f;
    [SerializeField] private float collapseFinishDelay = 2.6f;
    [SerializeField] private float gustImpulse = 1.6f;
    [SerializeField] private float gustSideVariation = 0.08f;
    [SerializeField] private float gustTorqueImpulse = 0.8f;
    [SerializeField] private float blockMass = 1f;

    private readonly List<BlockData> blocks = new List<BlockData>();

    private int blockCount = 0;
    private float balanceIndex = 0f;
    private bool isFalling = false;
    private int collapseDirection = 1;

    private Tween collapseStartTween;
    private Tween collapseFinishTween;

    public int BlockCount => blockCount;
    public float TowerHeight => blockCount * blockHeight;
    public bool IsFalling => isFalling;

    private void Start()
    {
        collapseDirection = UnityEngine.Random.value < 0.5f ? -1 : 1;
    }

    public void AddBlocks(int amount, Material blockMaterial)
    {
        if (isFalling)
            return;

        if (blockPrefab == null || blockMaterial == null)
        {
            return;
        }

        if (amount <= 0)
            return;

        for (int i = 0; i < amount; i++)
        {
            int index = blockCount;

            GameObject newBlock = Instantiate(blockPrefab, transform);

            newBlock.transform.localPosition = new Vector3(0f, index * blockHeight + blockHeight / 2f, 0f);
            newBlock.transform.localRotation = Quaternion.identity;

            Renderer[] renderers = newBlock.GetComponentsInChildren<Renderer>();

            foreach (Renderer blockRenderer in renderers)
            {
                blockRenderer.sharedMaterial = blockMaterial;
            }

            blocks.Add(new BlockData
            {
                gameObject = newBlock,
                blockTransform = newBlock.transform,
                originalPosition = newBlock.transform.localPosition,
                originalRotation = newBlock.transform.localRotation
            });

            newBlock.transform.localScale = Vector3.zero;
            newBlock.transform.DOScale(blockSize, 0.2f).SetDelay(i * blockDelay).SetEase(Ease.OutBack);

            blockCount++;
        }
    }

    public void SetBalance(float newBalance)
    {
        SetBalance(newBalance, null);
    }

    public void SetBalance(float newBalance, Action onCollapseComplete)
    {
        if (isFalling)
            return;

        balanceIndex = Mathf.Clamp(newBalance, 0f, 100f);

        if (balanceIndex >= 100f)
        {
            CollapseTower(onCollapseComplete);
        }
    }

    private void Update()
    {
        if (isFalling || blocks.Count == 0)
            return;

        float normalizedBalance = balanceIndex / 100f;

        float oscillation = Mathf.Sin(Time.time * swaySpeed) * normalizedBalance;

        for (int i = 0; i < blocks.Count; i++)
        {
            BlockData data = blocks[i];

            if (data.blockTransform == null)
                continue;

            float heightFactor = blocks.Count <= 1 ? 0f : (float)i / (blocks.Count - 1);
            float influence = Mathf.Pow(heightFactor, leanExponent);
            float directionalOffset = collapseDirection * maxTopOffset * normalizedBalance * influence;
            float oscillatingOffset = oscillation * maxTopSway * influence;
            float totalOffset = directionalOffset + oscillatingOffset;
            data.blockTransform.localPosition = data.originalPosition + Vector3.right * totalOffset;
            float leanAngle = collapseDirection * maxLeanAngle * normalizedBalance * influence;
            float swayAngle = oscillation * maxSway * influence;
            data.blockTransform.localRotation = data.originalRotation * Quaternion.Euler(0f, 0f, leanAngle + swayAngle);
        }
    }

    private void CollapseTower(Action onCollapseComplete)
    {
        if (isFalling)
            return;

        isFalling = true;

        Debug.Log("COLLAPSE STARTED | " + "Balance: " + balanceIndex + "%");

        collapseStartTween = DOVirtual.DelayedCall(
            collapseStartDelay,
            () =>
            {
                ApplyWindForce();

                collapseFinishTween = DOVirtual.DelayedCall(
                    collapseFinishDelay,
                    () =>
                    {
                        Debug.Log(
                            "[LUCKY STACK] COLLAPSE SEQUENCE FINISHED"
                        );

                        onCollapseComplete?.Invoke();
                    }
                );
            }
        );
    }

    private void ApplyWindForce()
    {
        Vector3 windDirection = transform.right * collapseDirection;

        int totalBlocks = blocks.Count;

        for (int i = 0; i < totalBlocks; i++)
        {
            BlockData data = blocks[i];

            if (data.gameObject == null)
                continue;

            Transform blockTransform = data.blockTransform;

            blockTransform.DOKill();
            blockTransform.SetParent(null, true);

            Collider[] colliders = data.gameObject.GetComponentsInChildren<Collider>();

            if (colliders.Length == 0)
            {
                data.gameObject.AddComponent<BoxCollider>();
            }
            else
            {
                foreach (Collider blockCollider in colliders)
                {
                    blockCollider.enabled = true;
                    blockCollider.isTrigger = false;
                }
            }

            Rigidbody rb = data.gameObject.GetComponent<Rigidbody>();

            if (rb == null)
            {
                rb = data.gameObject.AddComponent<Rigidbody>();
            }

            rb.mass = blockMass;
            rb.useGravity = true;
            rb.isKinematic = false;

            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            rb.maxAngularVelocity = 10f;

            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            float heightFactor = totalBlocks <= 1 ? 1f : (float)i / (totalBlocks - 1);
            float forceMagnitude = gustImpulse * heightFactor;
            float depthVariation = UnityEngine.Random.Range(-gustSideVariation, gustSideVariation);

            Vector3 smallVariation = transform.forward * depthVariation * heightFactor;

            rb.AddForce(windDirection * forceMagnitude + smallVariation, ForceMode.Impulse);

            float torque = -collapseDirection * gustTorqueImpulse * heightFactor;

            rb.AddTorque(Vector3.forward * torque, ForceMode.Impulse);
        }

        blocks.Clear();
    }

    private void OnDestroy()
    {
        collapseStartTween?.Kill();
        collapseFinishTween?.Kill();
    }
}
