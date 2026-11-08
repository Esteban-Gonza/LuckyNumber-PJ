using UnityEngine;

public class CameraTargetUpdater : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TowerController tower;
    [SerializeField] private Transform topAnchor;

    [Header("Settings")]
    [SerializeField] private float topOffset = 0f;

    private void LateUpdate()
    {
        if (tower == null || topAnchor == null)
            return;

        Vector3 targetPosition = tower.transform.position;
        targetPosition.y += tower.TowerHeight + topOffset;
        topAnchor.position = targetPosition;
    }
}
