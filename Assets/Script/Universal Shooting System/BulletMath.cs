using UnityEngine;

/// <summary>
/// 提供子弹相关的通用数学计算方法
/// </summary>
public static class BulletMath
{
    /// <summary>
    /// 计算能量武器随时间的衰减比例
    /// </summary>
    /// <param name="lifeTime">子弹总生命周期</param>
    /// <param name="currentTime">当前已存在时间</param>
    /// <param name="minRatio">最小衰减比例（如0.4表示最小保留40%强度）</param>
    /// <returns>衰减比例，范围[minRatio, 1]</returns>
    public static float GetEnergyDecayRatio(float lifeTime, float currentTime, float minRatio)
    {
        float ratio = Mathf.Clamp01(currentTime / Mathf.Max(lifeTime, 0.01f));
        return Mathf.Lerp(1f, minRatio, ratio);
    }

    /// <summary>
    /// 基于衰减比例计算能量弹实际伤害值
    /// </summary>
    public static float GetEnergyDamage(float baseDamage, float decayRatio)
    {
        return baseDamage * decayRatio;
    }

    /// <summary>
    /// 基于衰减比例计算能量弹穿甲等级（整数）
    /// </summary>
    public static int GetEnergyPenetrationLevel(int baseLevel, float decayRatio)
    {
        return Mathf.FloorToInt(baseLevel * decayRatio);
    }

    /// <summary>
    /// 基于衰减比例计算能量弹穿透力
    /// </summary>
    public static float GetEnergyPenetrationPower(float basePower, float decayRatio)
    {
        return basePower * decayRatio;
    }
}
