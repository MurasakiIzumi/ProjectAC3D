using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Camera))]
public class EnemySensor : MonoBehaviour
{
    [Header("锁定框放大比例")] public float Ratio = 0.1f;

    [Header("敌人默认锁定框颜色")] public Color EnemyFlameColor = Color.green;

    [Header("当前目标锁定框颜色")] public Color TargetFlameColor = Color.red;

    private List<GameObject> visibleEnemies = new List<GameObject>();  // 当前屏幕上可被锁定的敌人
    private GameObject selectedEnemy;                                  // 当前选中的敌人
    private Camera cam;

    void Awake()
    {
        cam = Camera.main; // 缓存摄像机引用，提升性能
    }

    void Update()
    {
        // 每帧刷新可锁定敌人列表
        visibleEnemies.Clear();
        GameObject[] allEnemies = GameObject.FindGameObjectsWithTag("Enemy");

        foreach (GameObject enemy in allEnemies)
        {
            if (IsVisibleToCameraAccurate(enemy))
            {
                visibleEnemies.Add(enemy);
            }
        }

        // 没有目标可选时清空选中目标
        if (visibleEnemies.Count == 0)
        {
            selectedEnemy = null;
            return;
        }

        // 当前选中目标不合法或为空 → 自动选中第一个
        if (selectedEnemy == null || !visibleEnemies.Contains(selectedEnemy))
        {
            selectedEnemy = visibleEnemies[0];
        }

        // 十字键左右切换锁定目标（按屏幕方向）
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            SelectEnemyInDirection(Vector2.right);
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            SelectEnemyInDirection(Vector2.left);
        }
    }

    void OnGUI()
    {
        foreach (GameObject enemy in visibleEnemies)
        {
            Renderer rend = enemy.GetComponentInChildren<Renderer>();
            if (rend == null) continue;

            Bounds bounds = rend.bounds;

            // 计算包围盒 8 个角在屏幕中的位置
            Vector3[] corners = new Vector3[8];
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;

            corners[0] = cam.WorldToScreenPoint(new Vector3(min.x, min.y, min.z));
            corners[1] = cam.WorldToScreenPoint(new Vector3(min.x, min.y, max.z));
            corners[2] = cam.WorldToScreenPoint(new Vector3(min.x, max.y, min.z));
            corners[3] = cam.WorldToScreenPoint(new Vector3(min.x, max.y, max.z));
            corners[4] = cam.WorldToScreenPoint(new Vector3(max.x, min.y, min.z));
            corners[5] = cam.WorldToScreenPoint(new Vector3(max.x, min.y, max.z));
            corners[6] = cam.WorldToScreenPoint(new Vector3(max.x, max.y, min.z));
            corners[7] = cam.WorldToScreenPoint(new Vector3(max.x, max.y, max.z));

            // 获取屏幕上的最小最大范围
            float minX = corners[0].x, maxX = corners[0].x;
            float minY = Screen.height - corners[0].y, maxY = Screen.height - corners[0].y;
            foreach (var c in corners)
            {
                float x = c.x;
                float y = Screen.height - c.y;
                minX = Mathf.Min(minX, x);
                maxX = Mathf.Max(maxX, x);
                minY = Mathf.Min(minY, y);
                maxY = Mathf.Max(maxY, y);
            }

            // 增加框边缘 padding（屏幕单位）
            float width = maxX - minX;
            float height = maxY - minY;
            float padX = width * Ratio;
            float padY = height * Ratio;

            Rect paddedRect = new Rect(
                minX - padX,
                minY - padY,
                width + 2 * padX,
                height + 2 * padY
            );

            bool isSelected = (enemy == selectedEnemy);
            Color boxColor = isSelected ? TargetFlameColor : EnemyFlameColor;

            DrawBox(paddedRect, boxColor, 2f);

            if (isSelected)
            {
                DrawLockLines(paddedRect, TargetFlameColor);
            }
        }
    }

    // 绘制锁定框（屏幕空间矩形）
    void DrawBox(Rect rect, Color color, float thickness)
    {
        Texture2D lineTex = Texture2D.whiteTexture;
        GUI.color = color;
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), lineTex); // 上
        GUI.DrawTexture(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), lineTex); // 下
        GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, rect.height), lineTex); // 左
        GUI.DrawTexture(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), lineTex); // 右
        GUI.color = Color.white;
    }

    // 绘制从目标向四边延伸的锁定线
    void DrawLockLines(Rect rect, Color color)
    {
        float cx = rect.center.x;
        float cy = rect.center.y;
        float thickness = 2f;
        Texture2D lineTex = Texture2D.whiteTexture;
        GUI.color = color;

        GUI.DrawTexture(new Rect(cx - thickness / 2, 0, thickness, rect.y), lineTex); // 上
        GUI.DrawTexture(new Rect(cx - thickness / 2, rect.yMax, thickness, Screen.height - rect.yMax), lineTex); // 下
        GUI.DrawTexture(new Rect(0, cy - thickness / 2, rect.x, thickness), lineTex); // 左
        GUI.DrawTexture(new Rect(rect.xMax, cy - thickness / 2, Screen.width - rect.xMax, thickness), lineTex); // 右

        GUI.color = Color.white;
    }

    // 根据给定方向（左右）选择屏幕中最近的敌人
    void SelectEnemyInDirection(Vector2 dir)
    {
        if (selectedEnemy == null) return;

        Vector3 currentScreenPos = cam.WorldToScreenPoint(selectedEnemy.GetComponent<Renderer>().bounds.center);

        GameObject closest = null;
        float bestScore = float.MaxValue;

        foreach (GameObject enemy in visibleEnemies)
        {
            if (enemy == selectedEnemy) continue;

            Vector3 screenPos = cam.WorldToScreenPoint(enemy.GetComponent<Renderer>().bounds.center);
            Vector2 toCandidate = (new Vector2(screenPos.x, screenPos.y) - new Vector2(currentScreenPos.x, currentScreenPos.y)).normalized;

            float dot = Vector2.Dot(toCandidate, dir.normalized);
            float distance = Vector2.Distance(new Vector2(screenPos.x, screenPos.y), new Vector2(currentScreenPos.x, currentScreenPos.y));

            if (dot > 0.5f && distance < bestScore)
            {
                bestScore = distance;
                closest = enemy;
            }
        }

        if (closest != null)
        {
            selectedEnemy = closest;
        }
    }

    // 判断目标是否处于摄像机视野中，且无遮挡（包括被敌人遮挡）
    bool IsVisibleToCameraAccurate(GameObject enemy)
    {
        Renderer rend = enemy.GetComponentInChildren<Renderer>();
        if (rend == null) return false;

        Vector3 screenPos = cam.WorldToViewportPoint(rend.bounds.center);

        if (screenPos.z < 0 || screenPos.x < 0 || screenPos.x > 1 || screenPos.y < 0 || screenPos.y > 1)
            return false;

        Vector3 eye = cam.transform.position;
        Vector3 dir = rend.bounds.center - eye;

        if (Physics.Raycast(eye, dir, out RaycastHit hit, dir.magnitude))
        {
            // 被任何物体（包括敌人）遮挡都算不可见
            return hit.collider.gameObject == enemy || hit.collider.transform.IsChildOf(enemy.transform);
        }

        return true;
    }

    // 获取当前选中的敌人（可用于攻击逻辑）
    public GameObject GetSelectedEnemy() => selectedEnemy;

    // 判断是否存在有效的目标
    public bool HasTarget => selectedEnemy != null;

    //调用方法
    //EnemyBoxDrawerDirectional sensor = FindObjectOfType<EnemyBoxDrawerDirectional>();
    //if (sensor.HasTarget)
    //{
    //GameObject target = sensor.GetSelectedEnemy();
    //]
}