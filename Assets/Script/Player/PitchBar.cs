using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// PitchBar（俯仰刻度条）
/// 用于控制一张垂直刻度图像在遮罩内上下移动，从而显示当前机体的俯仰角度。
/// 适用于简化HUD用途，非中心式仪表，默认刻度范围为±90°。
/// </summary>
public class PitchBar : MonoBehaviour
{
    [Header("参照对象")]
    [Tooltip("用于读取俯仰角的Transform，通常是Camera或机体")]
    public Transform pitchReference;

    [Header("UI组件")]
    [Tooltip("需要移动的刻度图Image（RectTransform）")]
    public RectTransform pitchImage;

    [Header("参数设置")]
    [Tooltip("每度偏移多少像素，需与贴图设计一致")]
    public float pixelsPerDegree = 10f;

    [Tooltip("俯仰角的显示范围（±），建议为90")]
    public float clampDegree = 90f;

    private void Update()
    {
        if (!pitchReference || !pitchImage) return;

        // 获取俯仰角度（转换为 -180 ~ 180）
        float pitch = pitchReference.eulerAngles.x;
        if (pitch > 180f) pitch -= 360f;

        // 限制角度范围
        float clampedPitch = Mathf.Clamp(pitch, -clampDegree, clampDegree);

        // 像素偏移
        float yOffset = clampedPitch * pixelsPerDegree;

        // 应用偏移
        pitchImage.anchoredPosition = new Vector2(0f, yOffset);
    }
}
