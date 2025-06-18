using UnityEngine;

/// <summary>
/// 测试用敌人伤害接收脚本，实现 IDamageReceiver
/// 支持护甲等级、HP 扣减、无效攻击特效触发、简单穿透反馈
/// </summary>
public class TestEnemyDamageHandler : MonoBehaviour, IDamageReceiver
{
    [Header("基础属性")]
    [Tooltip("敌人当前生命值")]
    public float currentHP = 100f;

    [Tooltip("敌人最大生命值")]
    public float maxHP = 100f;

    [Tooltip("敌人护甲等级（用于破甲判定）")]
    public int armorLevel = 2;

    [Header("特效设置")]
    [Tooltip("命中特效生成的位置（为空则使用命中点）")]
    public Transform effectSpawnPoint;

    private USS_Bullet lastBulletSource;

    public void ReceiveBulletHit(BulletHitInfo hitInfo)
    {
        bool isPenetrated = hitInfo.penetrationLevel > armorLevel;

        // 获取子弹来源中的 SpawnHitEffect 支持（用于特效播放）
        if (hitInfo.sourceWeapon != null)
        {
            lastBulletSource = hitInfo.sourceWeapon.GetComponent<USS_Bullet>();
        }

        // 攻击无效：穿甲等级不够
        if (!isPenetrated)
        {
            if (lastBulletSource != null)
            {
                lastBulletSource.SpawnHitEffect(GetEffectPosition(hitInfo), hitInfo.hitNormal, false);
                Vector3 inDir = hitInfo.sourceWeapon.transform.forward;
                lastBulletSource.OnDeflect(inDir, hitInfo.hitNormal);
            }
            Debug.Log($"⚠️ 攻击被装甲抵挡：穿甲 {hitInfo.penetrationLevel} <= 护甲 {armorLevel}");
            return;
        }

        // 播放有效命中特效
        if (lastBulletSource != null)
        {
            lastBulletSource.SpawnHitEffect(GetEffectPosition(hitInfo), hitInfo.hitNormal, true);
        }

        // 实弹伤害按穿甲优势衰减
        float finalDamage = hitInfo.damage;

        if (!hitInfo.isEnergy)
        {
            int delta = hitInfo.penetrationLevel - armorLevel;
            float ratio = Mathf.Clamp01(delta / 3f); // 3为穿甲饱和差
            float damageRatio = Mathf.Lerp(0.25f, 1f, ratio);
            finalDamage *= damageRatio;
        }

        // 扣血处理
        currentHP -= finalDamage;
        currentHP = Mathf.Clamp(currentHP, 0f, maxHP);

        Debug.Log($"✅ 命中有效：伤害 = {finalDamage:F1}，剩余HP = {currentHP:F1}");

        // 允许穿透命中的情况下通知子弹继续飞行
        if (hitInfo.canPenetrate && hitInfo.penetrationPower > armorLevel)
        {
            if (lastBulletSource != null)
            {
                Vector3 inDir = hitInfo.sourceWeapon.transform.forward;
                lastBulletSource.OnPenetrateContinue(inDir);
            }
        }

        // 破甲但未穿透时也进行偏转
        if (!hitInfo.canPenetrate || hitInfo.penetrationPower <= armorLevel)
        {
            if (lastBulletSource != null)
            {
                Vector3 inDir = hitInfo.sourceWeapon.transform.forward;
                lastBulletSource.OnDeflect(inDir, hitInfo.hitNormal);
            }
        }

        if (currentHP <= 0f)
        {
            Debug.Log("💥 敌人已被击毁！");
            Destroy(gameObject);
        }
    }

    private Vector3 GetEffectPosition(BulletHitInfo info)
    {
        return (effectSpawnPoint != null) ? effectSpawnPoint.position : info.hitPoint;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 1.5f, 0.25f);
    }
}
