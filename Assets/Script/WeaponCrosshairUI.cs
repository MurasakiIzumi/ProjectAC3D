using UnityEngine;
using UnityEngine.UI;

public class WeaponCrosshairUI : MonoBehaviour
{
    [Tooltip("准星图像（空心圆圈）")]
    public RectTransform crosshairRect;

    [Tooltip("HUD用Canvas（应为Screen Space - Camera或Overlay）")]
    public Canvas canvas;

    [Tooltip("准星投射距离（模拟子弹在多少米处命中位置）")]
    public float aimDistance = 30f;

    [Tooltip("每1度散布角对应准星像素大小（缩放倍率）")]
    public float angleToUIScale = 5f;

    [Tooltip("武器控制器（用于获取当前武器）")]
    public WeaponControl weaponControl;

    private USS_Weapon currentWeapon;

    void Update()
    {
        if (!weaponControl || !canvas || !crosshairRect) return;

        // 默认以右手武器为当前主武器来源（可根据实际逻辑调整）
        currentWeapon = GetPrimaryWeapon();
        if (currentWeapon == null || currentWeapon.muzzlePoint == null || currentWeapon.aimingCamera == null) return;

        // 计算准星世界位置（摄像机正前方向延伸）
        Vector3 aimPoint = currentWeapon.muzzlePoint.position + currentWeapon.muzzlePoint.forward * aimDistance;
        Vector3 screenPos = currentWeapon.aimingCamera.WorldToScreenPoint(aimPoint);

        // 将屏幕坐标转为Canvas本地坐标
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.GetComponent<RectTransform>(), screenPos, canvas.worldCamera, out Vector2 localPos))
        {
            crosshairRect.localPosition = localPos;
        }

        // 根据当前武器的散布角设置准星大小
        float spreadAngle = (currentWeapon.enableSpread) ? currentWeapon.spreadAngle : 0f;
        float size = spreadAngle * angleToUIScale;
        crosshairRect.sizeDelta = new Vector2(size, size);
    }

    // 获取当前使用中的主武器（此处默认取右手组第一把）
    private USS_Weapon GetPrimaryWeapon()
    {
        var group = weaponControl.rightGroup;
        if (group.weapons != null && group.weapons.Length > 0)
        {
            foreach (var w in group.weapons)
            {
                if (w is USS_Weapon uw) return uw;
            }
        }
        return null;
    }
}
