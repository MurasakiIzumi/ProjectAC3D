using UnityEngine;

public interface IWeapon
{
    void Fire();
    bool CanFire();
    float GetFireRate();
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
        group.weapons = new IWeapon[group.weaponComponents.Length];
        for (int i = 0; i < group.weaponComponents.Length; i++)
        {
            group.weapons[i] = group.weaponComponents[i] as IWeapon;
        }
    }

    private void HandleWeaponGroup(WeaponBinding group)
    {
        if (group.weapons == null || group.weapons.Length == 0) return;

        bool isHeld = Input.GetKey(group.fireKey);

        if (isHeld)
        {
            group.isFiring = true;

            if (Time.time >= group.nextFireTime)
            {
                bool hasFired = false;

                for (int i = 0; i < group.weapons.Length; i++)
                {
                    var w = group.weapons[i];
                    if (w == null)
                    {
                        Debug.LogWarning($"[WeaponControl] 武器{i} 是 null");
                        continue;
                    }

                    if (!w.CanFire())
                    {
                        Debug.Log($"[WeaponControl] 武器{i} 无法开火（CanFire=false）");
                        continue;
                    }

                    Debug.Log($"[WeaponControl] 触发武器{i} 开火");
                    w.Fire();
                    hasFired = true;
                }

                if (hasFired)
                {
                    float interval = 60f / group.weapons[0].GetFireRate();
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