using UnityEngine;
using System.Collections.Generic;

// 驾驶舱主监视器上的敌人框系统，仅在敌人处于主屏可视区域内时显示并允许锁定
public class EnemySensorUI : MonoBehaviour
{
    [Header("主监视器设定")]

    [Tooltip("Cockpit主视角用摄像机")]
    public Camera sensorCamera;

    [Tooltip("主监视器RawImage的RectTransform（作为可视区域判定）")]
    public RectTransform screenMaskArea;

    [Tooltip("敌人框的UI生成容器")]
    public Transform boxContainer;

    [Tooltip("敌人框的Prefab")]
    public EnemyBoxUI boxPrefab;

    [Header("距离缩放控制")]

    [Tooltip("敌人最大锁定距离（超出该距离的敌人不会被显示或锁定）")]
    public float maxLockDistance = 25f;

    [Tooltip("开始缩放的距离（该距离以内框体大小为最大值）")]
    public float startScaleDistance = 5f;

    [Tooltip("框体基础尺寸（以 scale = 1 为基准）")]
    public float baseBoxSize = 150f;

    [Header("敌人列表")]

    [Tooltip("当前场景中可锁定的敌人列表")]
    public List<Transform> enemyList = new List<Transform>();

    [Tooltip("当前锁定敌人的索引")]
    public int currentLockIndex = 0;

    // 内部缓存与控制
    private Dictionary<Transform, EnemyBoxUI> activeBoxes = new Dictionary<Transform, EnemyBoxUI>();
    private Queue<EnemyBoxUI> boxPool = new Queue<EnemyBoxUI>();

    private float switchCooldown = 0.2f;
    private float switchTimer = 0f;

    void Update()
    {
        // 清除失效敌人
        enemyList.RemoveAll(e => e == null || !e.gameObject.activeInHierarchy);
        switchTimer -= Time.deltaTime;

        HandleTargetSwitching();
        UpdateEnemyBox();

        // 锁定目标修正
        if (enemyList.Count > 0 && (currentLockIndex < 0 || currentLockIndex >= enemyList.Count))
        {
            currentLockIndex = 0;
        }

        if (currentLockIndex >= enemyList.Count)
            currentLockIndex = -1;
    }

    // 处理Q/E 或方向键切换锁定目标逻辑
    private void HandleTargetSwitching()
    {
        if (enemyList == null || enemyList.Count == 0)
        {
            currentLockIndex = -1;
            return;
        }

        // 如果当前没有任何可见目标 → 不响应切换
        bool hasVisibleTarget = false;
        foreach (var e in enemyList)
        {
            if (IsEnemyVisible(e))
            {
                hasVisibleTarget = true;
                break;
            }
        }

        if (!hasVisibleTarget)
        {
            currentLockIndex = -1;
            return;
        }

        bool left = Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.LeftArrow);
        bool right = Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.RightArrow);

        if ((left || right) && switchTimer <= 0f && enemyList.Count > 1)
        {
            int dir = right ? 1 : -1;
            int index = currentLockIndex;

            for (int i = 0; i < enemyList.Count; i++)
            {
                index = (index + dir + enemyList.Count) % enemyList.Count;
                if (IsEnemyVisible(enemyList[index]))
                {
                    currentLockIndex = index;
                    switchTimer = switchCooldown;
                    return;
                }
            }

            currentLockIndex = -1;
        }

        // 当前锁定失效 → 自动切换第一个可见目标
        if (currentLockIndex < 0 || currentLockIndex >= enemyList.Count || !IsEnemyVisible(enemyList[currentLockIndex]))
        {
            for (int i = 0; i < enemyList.Count; i++)
            {
                if (IsEnemyVisible(enemyList[i]))
                {
                    currentLockIndex = i;
                    return;
                }
            }

            currentLockIndex = -1;
        }
    }

    // 更新所有敌人框的位置与可见性
    private void UpdateEnemyBox()
    {
        foreach (Transform enemy in enemyList)
        {
            if (enemyList == null) return;
            if (enemy == null) continue;

            float distance = Vector3.Distance(sensorCamera.transform.position, enemy.position);
            if (distance > maxLockDistance)
            {
                DeactivateBox(enemy);
                continue;
            }

            Vector3 vp = sensorCamera.WorldToViewportPoint(enemy.position);
            if (vp.z < 0f || vp.x < 0f || vp.x > 1f || vp.y < 0f || vp.y > 1f)
            {
                DeactivateBox(enemy);
                continue;
            }

            float scale = 1f;
            if (distance > startScaleDistance)
            {
                float t = Mathf.InverseLerp(maxLockDistance, startScaleDistance, distance);
                scale = Mathf.Lerp(0.35f, 1f, t); // 可根据需要调整最小缩放值
            }

            float size = baseBoxSize * scale;
            float w = screenMaskArea.rect.width;
            float h = screenMaskArea.rect.height;

            Vector2 localPoint = new Vector2(
                (vp.x - 0.5f) * w,
                (vp.y - 0.5f) * h
            );

            EnemyBoxUI boxUI = GetOrCreateBox(enemy);
            boxUI.UpdateDistanceBox(localPoint, size);
            bool isLocked = (currentLockIndex >= 0 && currentLockIndex < enemyList.Count && enemy == enemyList[currentLockIndex]);
            boxUI.SetLocked(isLocked);
        }

        // 清理多余框体
        List<Transform> keys = new List<Transform>(activeBoxes.Keys);
        foreach (var t in keys)
        {
            if (!enemyList.Contains(t))
                DeactivateBox(t);
        }
    }

    private EnemyBoxUI GetOrCreateBox(Transform enemy)
    {
        if (activeBoxes.TryGetValue(enemy, out EnemyBoxUI box))
            return box;

        EnemyBoxUI newBox = boxPool.Count > 0 ? boxPool.Dequeue() : Instantiate(boxPrefab, boxContainer);
        newBox.Initialize(enemy);
        activeBoxes[enemy] = newBox;
        return newBox;
    }

    private void DeactivateBox(Transform enemy)
    {
        if (activeBoxes.TryGetValue(enemy, out EnemyBoxUI box))
        {
            box.gameObject.SetActive(false);
            activeBoxes.Remove(enemy);
            boxPool.Enqueue(box);
        }
    }

    // 判断敌人是否在屏幕Mask区域内（加边距以避免误判）
    private bool IsEnemyVisible(Transform enemy)
    {
        if (enemy == null) return false;

        Vector3 vp = sensorCamera.WorldToViewportPoint(enemy.position);
        if (vp.z < 0f || vp.x < 0f || vp.x > 1f || vp.y < 0f || vp.y > 1f)
            return false;

        float w = screenMaskArea.rect.width;
        float h = screenMaskArea.rect.height;

        Vector2 localPoint = new Vector2(
            (vp.x - 0.5f) * w,
            (vp.y - 0.5f) * h
        );

        Rect safeRect = screenMaskArea.rect;
        float margin = 220f;
        safeRect.xMin += margin;
        safeRect.xMax -= margin;
        safeRect.yMin += margin;
        safeRect.yMax -= margin;

        return safeRect.Contains(localPoint);
    }

    // 获取当前锁定敌人（兼容GUI接口）
    public GameObject GetSelectedEnemy()
    {
        if (currentLockIndex >= 0 && currentLockIndex < enemyList.Count)
            return enemyList[currentLockIndex]?.gameObject;
        return null;
    }

    // 是否存在有效目标（兼容GUI接口）
    public bool HasTarget => GetSelectedEnemy() != null;
}
