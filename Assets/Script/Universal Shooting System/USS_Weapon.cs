using UnityEngine;

public enum FireMode
{
    Auto,
    SemiAuto
}

[RequireComponent(typeof(AudioSource))]
public class USS_Weapon : MonoBehaviour, IWeapon, IWeaponContinuous
{
    [Header("基础设定")]
    [Tooltip("当前武器的射击模式")]
    public FireMode fireMode = FireMode.Auto;
    public FireMode GetFireMode() => fireMode;

    [Tooltip("半自动模式下的最大射速（发/秒）")]
    public float semiAutoMaxRate = 3f;

    [Tooltip("自动武器的射速（发/秒）")]
    public float autoFireRate = 10f;

    [Tooltip("每次射击消耗的子弹数")]
    public int ammoPerShot = 1;

    [Tooltip("是否在弹尽时自动丢弃武器")]
    public bool discardOnEmpty = false;

    [Header("弹药设定")]
    [Tooltip("当前弹药数量（对能量武器为当前能量；对实弹为弹匣剩余）")]
    public int currentAmmo = 30;

    [Tooltip("单个弹匣容量（仅实弹武器使用）")]
    public int magazineSize = 30;

    [Tooltip("备弹数量（仅实弹武器使用）")]
    public int backupAmmo = 90;

    [Tooltip("能量武器最大弹容量（用于UI显示，非实弹武器可设为100等）")]
    public int maxEnergyAmmo = 100;

    [Tooltip("是否启用能量自动回复（如为能量武器请开启）")]
    public bool autoRecoverAmmo = false;

    [Tooltip("能量回复速度（单位：每秒）")]
    public float recoverRate = 1f;

    [Tooltip("射击后等待多久才开始恢复能量（秒）")]
    public float energyRecoverDelay = 1.5f;

    [Tooltip("是否使用手动换弹机制（如为能量武器请关闭）")]
    public bool usesReload = true;

    [Tooltip("换弹耗时（秒）")]
    public float reloadDuration = 2f;

    [Header("枪口旋转设定")]
    [SerializeField, Tooltip("敌人锁定系统（EnemySensorUI）")]
    private EnemySensorUI sensorUI;

    [Tooltip("是否使用自动瞄准（锁定目标）")]
    public bool useAutoAim = true;

    [Tooltip("武器朝向的旋转速度（度/秒）")]
    public float aimRotateSpeed = 180f;

    [Tooltip("允许的最大水平旋转角度（度）")]
    public float maxYaw = 30f;

    [Tooltip("允许的最大垂直旋转角度（度）")]
    public float maxPitch = 15f;

    [Tooltip("用于旋转的武器挂点")]
    public Transform weaponPivot;

    [Tooltip("主监视机")]
    public Camera aimingCamera;

    [Header("发射点")]
    [Tooltip("子弹生成点")]
    public Transform muzzlePoint;

    [Tooltip("要生成的子弹Prefab")]
    public GameObject bulletPrefab;

    [Header("弹道散布")]
    [Tooltip("是否启用弹道散布")]
    public bool enableSpread = false;

    [Tooltip("最大散布角度（度）")]
    public float spreadAngle = 3f;

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
    private bool isLooping;
    private AudioSource audioSource;

    private bool isReloading = false;
    private float reloadEndTime = 0f;
    private float ammoRecoverBuffer = 0f;
    private float lastFireTime = -999f;
    public bool IsReloading => isReloading;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (usesReload)
        {
            currentAmmo = magazineSize;
        }
        else
        {
            currentAmmo = maxEnergyAmmo;
        }
    }

    void Update()
    {
        UpdateAiming();

        // 能量型武器弹药回复
        if (autoRecoverAmmo && currentAmmo < maxEnergyAmmo && Time.time >= lastFireTime + energyRecoverDelay)
        {
            ammoRecoverBuffer += recoverRate * Time.deltaTime;

            if (ammoRecoverBuffer >= 1f)
            {
                int gained = Mathf.FloorToInt(ammoRecoverBuffer);
                currentAmmo = Mathf.Min(currentAmmo + gained, maxEnergyAmmo);
                ammoRecoverBuffer -= gained;
            }
        }

        // 换弹完成逻辑（仅对 usesReload 武器生效）
        if (isReloading && Time.time >= reloadEndTime)
        {
            isReloading = false;

            int needed = magazineSize - currentAmmo;
            int reloadAmount = Mathf.Min(needed, backupAmmo);

            currentAmmo += reloadAmount;
            backupAmmo -= reloadAmount;
        }

        // 自动武器循环段音效维护
        if (fireMode == FireMode.Auto && isLooping)
        {
            if(currentAmmo < ammoPerShot || isReloading)
            {
                Debug.Log("0");
                StopLoopSound();
            }
            else if (audioSource.time >= loopEndTime)
            {
                Debug.Log("1");
                audioSource.time = loopStartTime;
            }
        }
    }

    private void UpdateAiming()
    {
        if (weaponPivot == null) return;

        Quaternion targetRotation;

        if (useAutoAim && sensorUI != null && sensorUI.IsSelectedTargetVisible())
        {
            GameObject targetObject = sensorUI.GetSelectedEnemy();
            if (targetObject != null)
            {
                // 自瞄模式：朝向锁定目标，使用Yaw/Pitch限制
                Vector3 dirToTarget = targetObject.transform.position - weaponPivot.position;

                Vector3 localDir = transform.InverseTransformDirection(dirToTarget.normalized);
                float yaw = Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg;
                float pitch = -Mathf.Asin(localDir.y) * Mathf.Rad2Deg;
                yaw = Mathf.Clamp(yaw, -maxYaw, maxYaw);
                pitch = Mathf.Clamp(pitch, -maxPitch, maxPitch);
                Vector3 clampedDir = Quaternion.Euler(pitch, yaw, 0f) * Vector3.forward;
                Vector3 worldClampedDir = transform.TransformDirection(clampedDir);
                targetRotation = Quaternion.LookRotation(worldClampedDir);
            }
            else
            {
                targetRotation = Quaternion.LookRotation(transform.forward);
            }
        }
        else
        {
            // 非自瞄模式：朝向摄像机前方（不做角度限制）
            Camera cam = aimingCamera != null ? aimingCamera : Camera.main;
            if (cam != null)
            {
                Vector3 dirToCamForward = cam.transform.forward;
                targetRotation = Quaternion.LookRotation(dirToCamForward);
            }
            else
            {
                targetRotation = Quaternion.LookRotation(transform.forward);
            }
        }

        float aimSmoothFactor = 10f;
        weaponPivot.rotation = Quaternion.Slerp(weaponPivot.rotation, targetRotation, Time.deltaTime * aimSmoothFactor);
    }

    public bool CanFire()
    {
        return currentAmmo >= ammoPerShot && !isReloading && Time.time >= nextFireTime;
    }

    public float GetFireRate()
    {
        float rps = fireMode == FireMode.Auto ? autoFireRate : semiAutoMaxRate;
        return rps * 60f;
    }

    public void Fire()
    {
        if (!CanFire()) return;

        if (bulletPrefab && muzzlePoint)
        {
            Quaternion spreadRotation = muzzlePoint.rotation;

            if (enableSpread && spreadAngle > 0f)
            {
                Vector2 randomCircle = Random.insideUnitCircle * Mathf.Tan(spreadAngle * Mathf.Deg2Rad);
                Vector3 spreadDir = muzzlePoint.forward +
                                    muzzlePoint.right * randomCircle.x +
                                    muzzlePoint.up * randomCircle.y;

                spreadRotation = Quaternion.LookRotation(spreadDir.normalized);
            }

            Instantiate(bulletPrefab, muzzlePoint.position, spreadRotation);
        }

        currentAmmo -= ammoPerShot;

        if (discardOnEmpty && usesReload && (currentAmmo + backupAmmo) <= 0)
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
        lastFireTime = Time.time;
    }

    public void Reload()
    {
        if (!usesReload || isReloading || currentAmmo >= magazineSize || backupAmmo <= 0) return;

        isReloading = true;
        reloadEndTime = Time.time + reloadDuration;
    }

    public void StopLoopSound()
    {
        if (isLooping)
        {
            isLooping = false;
            audioSource.time = loopEndTime;
        }
    }

    public void Stop()
    {
        StopLoopSound();
    }
}
