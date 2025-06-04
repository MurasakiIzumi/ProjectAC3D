using UnityEngine;
using UnityEngine.UI;

// 敌人框体UI组件，负责更新其位置、尺寸与颜色
public class EnemyBoxUI : MonoBehaviour
{
    [Tooltip("框体的RectTransform")]
    public RectTransform rectTransform;

    [Tooltip("边框Image组件")]
    public Image frameImage;

    [Tooltip("锁定目标时的颜色")]
    public Color lockedColor = Color.red;

    [Tooltip("普通状态下的颜色")]
    public Color normalColor = Color.green;

    private Transform target;

    // 初始化并绑定该框体的目标
    public void Initialize(Transform target)
    {
        this.target = target;
    }

    // 设置是否为当前锁定目标，切换颜色
    public void SetLocked(bool isLocked)
    {
        if (frameImage != null)
            frameImage.color = isLocked ? lockedColor : normalColor;
    }

    // 更新框体的位置与尺寸
    public void UpdateDistanceBox(Vector2 anchoredPos, float size)
    {
        rectTransform.anchoredPosition = anchoredPos;
        rectTransform.sizeDelta = new Vector2(size, size);
        gameObject.SetActive(true);
    }
}
