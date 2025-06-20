using UnityEngine;
using UnityEngine.UI;

public class HPStatus : MonoBehaviour
{
    [Tooltip("玩家血量数据源")]
    public PlayerMechHealth mechHealth;

    public enum MechPart
    {
        Head,
        Body,
        Foot,
        RightHand,
        LeftHand
    }

    [Tooltip("该图像代表的机体部位")]
    public MechPart targetPart;

    [Tooltip("高于该比率时使用正常颜色")]
    [Range(0f, 1f)] public float healthyThreshold = 0.5f;

    [Tooltip("高于该比率但低于 healthyThreshold 时使用受损颜色")]
    [Range(0f, 1f)] public float damagedThreshold = 0.2f;

    [Tooltip("低于该比率时闪烁")]
    [Range(0f, 1f)] public float criticalFlashThreshold = 0.05f;

    [Tooltip("闪烁频率（次/秒）")]
    public float flashFrequency = 2f;

    [Header("颜色设定")]
    public Color healthyColor = Color.white;
    public Color damagedColor = new Color(1f, 0.65f, 0f);
    public Color criticalColor = Color.red;

    private Image image;
    private float flashTimer = 0f;
    private bool flashOn = true;

    private bool isInCritical = false;
    private HPAlert alertManager;
    private static readonly Color zeroHPColor = Color.black;

    void Awake()
    {
        image = GetComponent<Image>();
        if (image == null)
            Debug.LogWarning("HPStatus 未找到 Image 组件", this);

        alertManager = FindObjectOfType<HPAlert>();
    }

    void Update()
    {
        if (mechHealth == null || image == null) return;

        float hp = GetPartHP();
        float ratio = Mathf.Clamp01(hp / mechHealth.MaxHp);

        // 死亡：强制黑色
        if (hp <= 0f)
        {
            image.color = zeroHPColor;
            ExitCritical();
            return;
        }

        // 临界状态：闪烁 + 注册警报
        if (ratio <= criticalFlashThreshold)
        {
            EnterCritical();
            HandleFlashing();
            return;
        }

        // 非临界：正常颜色
        ExitCritical();

        if (ratio > healthyThreshold)
            image.color = healthyColor;
        else if (ratio > damagedThreshold)
            image.color = damagedColor;
        else
            image.color = criticalColor;
    }

    void HandleFlashing()
    {
        flashTimer += Time.deltaTime;
        float interval = 1f / flashFrequency;

        if (flashTimer >= interval)
        {
            flashTimer = 0f;
            flashOn = !flashOn;
        }

        image.color = flashOn ? criticalColor : new Color(criticalColor.r, criticalColor.g, criticalColor.b, 0f);
    }

    float GetPartHP()
    {
        switch (targetPart)
        {
            case MechPart.Head: return mechHealth.HeadHP;
            case MechPart.Body: return mechHealth.BodyHP;
            case MechPart.Foot: return mechHealth.FootHP;
            case MechPart.RightHand: return mechHealth.RightHandHP;
            case MechPart.LeftHand: return mechHealth.LeftHandHP;
            default: return 0f;
        }
    }

    void EnterCritical()
    {
        if (!isInCritical)
        {
            isInCritical = true;
            alertManager?.RegisterCritical(this);
        }
    }

    void ExitCritical()
    {
        if (isInCritical)
        {
            isInCritical = false;
            alertManager?.UnregisterCritical(this);
        }
    }
}
