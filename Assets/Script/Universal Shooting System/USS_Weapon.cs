using UnityEngine;

public enum FireMode
{
    Auto,
    SemiAuto
}

[RequireComponent(typeof(AudioSource))]
public class USS_Weapon : MonoBehaviour, IWeapon
{
    [Header("基础设定")]
    [Tooltip("当前武器的射击模式")]
    public FireMode fireMode = FireMode.Auto;
    public FireMode GetFireMode() => fireMode;

    [Tooltip("半自动模式下的最大射速（发/秒）")]
    public float semiAutoMaxRate = 3f;

    [Tooltip("自动武器的射速（发/秒）")]
    public float autoFireRate = 10f;

    [Tooltip("是否在弹尽时自动丢弃武器")]
    public bool discardOnEmpty = false;

    [Header("弹药设定")]
    [Tooltip("最大弹容量")]
    public int maxAmmo = 30;

    [Tooltip("当前弹药数量")]
    public int currentAmmo = 30;

    [Tooltip("是否启用能量自动回复")]
    public bool autoRecoverAmmo = false;

    [Tooltip("能量回复速度（单位：每秒）")]
    public float recoverRate = 1f;

    [Header("枪口旋转设定")]
    [SerializeField, Tooltip("敌人锁定系统（EnemySensorUI）")]
    private EnemySensorUI sensorUI;

    [Tooltip("武器朝向的旋转速度（度/秒）")]
    public float aimRotateSpeed = 180f;

    [Tooltip("允许的最大水平旋转角度（度）")]
    public float maxYaw = 30f;

    [Tooltip("允许的最大垂直旋转角度（度）")]
    public float maxPitch = 15f;

    [Tooltip("用于旋转的武器挂点")]
    public Transform weaponPivot;

    [Header("发射点")]
    [Tooltip("子弹生成点")]
    public Transform muzzlePoint;

    [Tooltip("要生成的子弹Prefab")]
    public GameObject bulletPrefab;

    [Header("音效控制")]
    [Tooltip("完整音效剪辑（含起始音、循环段、尾音）")]
    public AudioClip autoFireSE;

    [Tooltip("循环段开始时间（秒）")]
    public float loopStartTime;

    [Tooltip("循环段结束时间（秒）")]
    public float loopEndTime;

    [Tooltip("半自动武器每次射击时的音效")]
    public AudioClip semiAutoFireSE;

    private float nextFireTime;
    private float loopTimer;
    private bool isLooping;
    private AudioSource audioSource;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        UpdateAiming();
        if (autoRecoverAmmo && currentAmmo < maxAmmo)
        {
            currentAmmo += Mathf.FloorToInt(recoverRate * Time.deltaTime);
            currentAmmo = Mathf.Min(currentAmmo, maxAmmo);
        }

        if (fireMode == FireMode.Auto && isLooping)
        {
            if (audioSource.time >= loopEndTime)
            {
                audioSource.time = loopStartTime;
            }
        }
    }

    private void UpdateAiming()
    {
        if (sensorUI == null || weaponPivot == null) return;

        GameObject targetObject = sensorUI.GetSelectedEnemy();
        if (targetObject == null) return;

        Vector3 dirToTarget = targetObject.transform.position - weaponPivot.position;
        Quaternion targetRot = Quaternion.LookRotation(dirToTarget);

        // 角度限制处理（基于本体朝向，限制旋转范围）
        Vector3 forward = transform.forward;
        Vector3 localDir = transform.InverseTransformDirection(dirToTarget.normalized);

        float yaw = Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg;
        float pitch = -Mathf.Asin(localDir.y) * Mathf.Rad2Deg;

        yaw = Mathf.Clamp(yaw, -maxYaw, maxYaw);
        pitch = Mathf.Clamp(pitch, -maxPitch, maxPitch);

        Vector3 clampedDir = Quaternion.Euler(pitch, yaw, 0f) * Vector3.forward;
        Vector3 worldClampedDir = transform.TransformDirection(clampedDir);

        Quaternion clampedRotation = Quaternion.LookRotation(worldClampedDir);

        weaponPivot.rotation = Quaternion.RotateTowards(
            weaponPivot.rotation,
            clampedRotation,
            aimRotateSpeed * Time.deltaTime
        );
    }


    public bool CanFire()
    {
        return currentAmmo > 0 && Time.time >= nextFireTime;
    }

    public float GetFireRate()
    {
        float rps = fireMode == FireMode.Auto ? autoFireRate : semiAutoMaxRate;
        return rps * 60f;  // 转换为发/分钟
    }

    public void Fire()
    {
        if (!CanFire()) return;

        if (bulletPrefab && muzzlePoint)
        {
            Instantiate(bulletPrefab, muzzlePoint.position, muzzlePoint.rotation);
        }

        currentAmmo--;

        if (discardOnEmpty && currentAmmo <= 0)
        {
            Destroy(gameObject);
            return;
        }

        if (fireMode == FireMode.Auto)
        {
            if (!isLooping)
            {
                audioSource.clip = autoFireSE;
                audioSource.loop = false;
                audioSource.Play();
                isLooping = true;
            }
        }
        else if (fireMode == FireMode.SemiAuto)
        {
            audioSource.PlayOneShot(semiAutoFireSE);
        }

        float interval = 1f / GetFireRate();
        nextFireTime = Time.time + interval;
    }

    public void StopLoopSound()
    {
        if (isLooping)
        {
            isLooping = false;
            audioSource.loop = false;
        }
    }
}
