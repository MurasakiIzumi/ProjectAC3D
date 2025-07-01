using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class DirectionIndicator : MonoBehaviour
{
    [Header("玩家参考")]
    [Tooltip("玩家的摄像机或主控物体，用于获取朝向")]
    public Transform playerTransform;

    [Header("UI图像")]
    [Tooltip("方向图背景图（要循环滚动）")]
    public RectTransform compassImage;

    [Tooltip("敌人方向图标的父级容器（放在Mask内）")]
    public RectTransform iconContainer;
    [Tooltip("敌人方向图标预制体")]
    public GameObject enemyIconPrefab;

    [Header("设定")]
    [Tooltip("每度对应滚动的像素数")]
    public float pixelsPerDegree = 2f;
    [Tooltip("方向图中一轮（360°）的宽度")]
    public float singleLoopWidth = 720f;

    [Header("敌人列表")]
    [Tooltip("敌人目标列表（建议由其他系统同步管理）")]
    public List<Transform> enemyTargets = new List<Transform>();

    private float totalImageWidth;
    private List<GameObject> activeIcons = new List<GameObject>();

    void Start()
    {
        totalImageWidth = compassImage.rect.width;
    }

    void Update()
    {
        if (playerTransform == null) return;

        // 计算玩家Yaw角度（0~360）
        float yaw = playerTransform.eulerAngles.y;

        // 滚动方向图背景图像
        float offsetX = -(yaw * pixelsPerDegree) % singleLoopWidth;
        compassImage.anchoredPosition = new Vector2(offsetX, compassImage.anchoredPosition.y);

        UpdateEnemyIcons(yaw);
    }

    void UpdateEnemyIcons(float playerYaw)
    {
        // 清理已有图标
        foreach (var icon in activeIcons)
            Destroy(icon);
        activeIcons.Clear();

        foreach (var enemy in enemyTargets)
        {
            if (enemy == null) continue;

            Vector3 toEnemy = enemy.position - playerTransform.position;
            toEnemy.y = 0f; // 只考虑水平角度

            float angle = Vector3.SignedAngle(playerTransform.forward, toEnemy, Vector3.up);
            float iconX = angle * pixelsPerDegree;

            GameObject icon = Instantiate(enemyIconPrefab, iconContainer);
            icon.GetComponent<RectTransform>().anchoredPosition = new Vector2(iconX, 0);
            activeIcons.Add(icon);
        }
    }
}