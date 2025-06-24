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

    [Tooltip("准星检测时忽略的Layer")]
    public LayerMask ignoreLayers;

    [Tooltip("武器控制器（用于获取当前武器）")]
    public WeaponControl weaponControl;

    private USS_Weapon currentWeapon;

    void Update()
    {
        if (!weaponControl || !canvas || !crosshairRect) return;

        currentWeapon = GetPrimaryWeapon();
        if (currentWeapon == null || currentWeapon.muzzlePoint == null || currentWeapon.aimingCamera == null) return;

        // 用枪口位置发射 Raycast，检测实际命中点
        Vector3 aimPoint;
        RaycastHit hit;
        Ray ray = new Ray(currentWeapon.muzzlePoint.position, currentWeapon.muzzlePoint.forward);
        if (Physics.Raycast(ray, out hit, 1000f, ~ignoreLayers))
        {
            aimDistance = hit.distance;
            aimPoint = hit.point;
        }
        else
        {
            aimDistance = 30f;
            aimPoint = ray.origin + ray.direction * aimDistance;
        }

        // 将命中点转换为屏幕坐标 → Canvas坐标
        Vector3 screenPos = currentWeapon.aimingCamera.WorldToScreenPoint(aimPoint);
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.GetComponent<RectTransform>(), screenPos, canvas.worldCamera, out Vector2 localPos))
        {
            crosshairRect.localPosition = localPos;
        }

        // 准星大小控制
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
