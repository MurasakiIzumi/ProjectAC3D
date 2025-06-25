using TMPro;
using UnityEngine;

public class GatlingAmmoHUD : MonoBehaviour
{
    [Header("UI 显示组件")]
    [Tooltip("用于显示剩余弹药数的 TMP Text")]
    public TextMeshProUGUI ammoText;

    [Header("火神炮模块引用（支持最多两个）")]
    [Tooltip("火神炮模块 A")]
    public SS_GatlingModule gatlingA;

    [Tooltip("火神炮模块 B")]
    public SS_GatlingModule gatlingB;

    [Header("显示倍率设置")]
    [Tooltip("每发弹药在视觉上显示为几发")]
    public int visualMultiplier = 4;

    [Header("颜色设定")]
    [Tooltip("默认颜色（≥50%）")]
    public Color normalColor = Color.white;

    [Tooltip("警告颜色（<50%）")]
    public Color warningColor = new Color(1f, 0.6f, 0f); // 橙色

    [Tooltip("严重警告颜色（<10%）")]
    public Color dangerColor = Color.red;

    [Header("闪烁设置")]
    [Tooltip("低弹量闪烁频率（次/秒）")]
    public float flashFrequency = 2f;

    private int maxAmmo = -1;
    private int displayedAmmo = 0;
    private float flashTimer = 0f;
    private bool flashVisible = true;

    void Start()
    {
        int totalMax = 0;
        if (gatlingA != null) totalMax += gatlingA.ammoCount;
        if (gatlingB != null) totalMax += gatlingB.ammoCount;
        maxAmmo = totalMax > 0 ? totalMax * visualMultiplier : -1;

        // 初始化显示值
        displayedAmmo = maxAmmo;
    }

    void Update()
    {
        int totalAmmo = 0;
        bool hasAny = false;

        if (gatlingA != null)
        {
            totalAmmo += gatlingA.GetRemainingAmmo();
            hasAny = true;
        }

        if (gatlingB != null)
        {
            totalAmmo += gatlingB.GetRemainingAmmo();
            hasAny = true;
        }

        int targetVisualAmmo = totalAmmo * visualMultiplier;

        if (ammoText != null)
        {
            if (!hasAny || maxAmmo <= 0)
            {
                ammoText.text = "--";
                ammoText.color = normalColor;
                return;
            }

            // 缓慢逼近目标值（逐发递减）
            if (displayedAmmo > targetVisualAmmo)
                displayedAmmo -= 1;
            else if (displayedAmmo < targetVisualAmmo)
                displayedAmmo = targetVisualAmmo; // 若加子弹则立即更新

            ammoText.text = "Ammo: "+displayedAmmo.ToString("D4");

            float percent = (float)displayedAmmo / maxAmmo;

            if (percent < 0.1f)
            {
                // 闪烁逻辑
                flashTimer += Time.deltaTime;
                if (flashTimer >= 1f / flashFrequency)
                {
                    flashTimer = 0f;
                    flashVisible = !flashVisible;
                }

                ammoText.color = flashVisible ? dangerColor : new Color(dangerColor.r, dangerColor.g, dangerColor.b, 0f);
            }
            else
            {
                // 重置闪烁状态
                flashTimer = 0f;
                flashVisible = true;

                if (percent < 0.5f)
                    ammoText.color = warningColor;
                else
                    ammoText.color = normalColor;
            }
        }
    }
}