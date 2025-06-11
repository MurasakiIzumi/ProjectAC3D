using System.Collections.Generic;
using UnityEngine;

public interface IWeapon
{
    void Fire();
    bool CanFire();
    float GetFireRate();
    FireMode GetFireMode();
}

// 可选支持 Stop() 的持续型武器（如火神炮、盾牌等）
public interface IWeaponContinuous : IWeapon
{
    void Stop();
}

[System.Serializable]
public class WeaponBinding
{
    [Tooltip("用于触发该武器的按键")]
    public KeyCode fireKey = KeyCode.None;

    [Tooltip("该组包含的多个武装组件（会交替触发）")]
    public MonoBehaviour[] weaponComponents;

    [HideInInspector] public IWeapon[] weapons;
    [HideInInspector] public int currentWeaponIndex = 0;
    [HideInInspector] public float nextFireTime = 0f;
    [HideInInspector] public bool isFiring = false;
}

public class WeaponControl : MonoBehaviour
{
    [Header("武器组绑定")]
    [Tooltip("火神炮组（如头部双管）")]
    public WeaponBinding gatlingGroup;

    [Tooltip("左手武装组")]
    public WeaponBinding leftGroup;

    [Tooltip("右手武装组")]
    public WeaponBinding rightGroup;

    [Header("通用换弹设置")]
    [Tooltip("换弹功能按键（需与射击键组合使用）")]
    public KeyCode reloadModifierKey = KeyCode.R;

    private void Awake()
    {
        InitWeaponGroup(gatlingGroup);
        InitWeaponGroup(leftGroup);
        InitWeaponGroup(rightGroup);
    }

    private void Update()
    {
        HandleWeaponGroup(gatlingGroup);
        HandleWeaponGroup(leftGroup);
        HandleWeaponGroup(rightGroup);
    }

    private void InitWeaponGroup(WeaponBinding group)
    {
        var validWeapons = new List<IWeapon>();

        foreach (var mb in group.weaponComponents)
        {
            if (mb != null && mb.gameObject.activeInHierarchy && mb is IWeapon w)
            {
                validWeapons.Add(w);
            }
        }

        group.weapons = validWeapons.ToArray();
    }

    private void HandleWeaponGroup(WeaponBinding group)
    {
        if (group.weapons == null || group.weapons.Length == 0) return;

        // 默认读取第一把武器的模式（假设该组武器 FireMode 一致）
        var firstWeapon = group.weapons[0];
        if (firstWeapon == null) return;

        FireMode mode = FireMode.Auto;
        if (firstWeapon is USS_Weapon uw)
        {
            mode = uw.GetFireMode();
        }

        // 检查是否触发换弹（组合键：换弹键 + 射击键）
        if (Input.GetKey(reloadModifierKey) && Input.GetKeyDown(group.fireKey))
        {
            foreach (var w in group.weapons)
            {
                if (w is USS_Weapon weapon && weapon.usesReload)
                {
                    weapon.Reload();
                }
            }
            return; // 本帧跳过射击逻辑
        }

        // 按当前武器模式选择输入方式
        bool wantsToFire =
            mode == FireMode.Auto ? Input.GetKey(group.fireKey) :
            mode == FireMode.SemiAuto ? Input.GetKeyDown(group.fireKey) :
            false;

        if (wantsToFire)
        {
            group.isFiring = true;

            if (Time.time >= group.nextFireTime)
            {
                bool hasFired = false;

                for (int i = 0; i < group.weapons.Length; i++)
                {
                    var w = group.weapons[i];
                    if (w == null) continue;
                    if (!w.CanFire()) continue;

                    w.Fire();
                    hasFired = true;
                }

                if (hasFired)
                {
                    float interval = 60f / firstWeapon.GetFireRate();
                    group.nextFireTime = Time.time + interval;
                }
            }
        }
        else if (group.isFiring)
        {
            foreach (var w in group.weapons)
            {
                if (w is IWeaponContinuous cont) cont.Stop();
            }

            group.isFiring = false;
        }
    }
}
