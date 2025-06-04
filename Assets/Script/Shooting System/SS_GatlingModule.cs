using UnityEngine;

public class SS_GatlingModule : MonoBehaviour, IWeaponContinuous
{
    [Header("基础参数")]
    [Tooltip("火神炮最大射程（米）")]
    public float fireRange = 80f;

    [Tooltip("每分钟射速（视觉表现频率）")]
    public float visualFireRate = 600f;

    [Tooltip("初始弹药数（内部系统将以 4x 速率消耗）")]
    public int ammoCount = 300;

    [Header("方向控制")]
    [Tooltip("主监视器摄像机（决定射击方向）")]
    public Camera mainCamera;

    [Tooltip("射击起始点（即炮口位置）")]
    public Transform firePoint;

    [Header("视觉表现")]
    [Tooltip("曳光轨迹FX控制器")]
    public SS_GatlingTracerFX tracerFX;

    [Tooltip("命中表现FX控制器")]
    public SS_GatlingImpactFX impactFX;

    [Header("射击音效")]
    [Tooltip("从第几秒开始进入循环区段")]
    public float loopStartTime = 0.3f;

    [Tooltip("从第几秒开始是尾响段")]
    public float tailStartTime = 4.8f;

    [Tooltip("完整音频")]
    public AudioSource fullClipSource;

    private bool isFiring = false;

    public void Fire()
    {
        if (!CanFire()) return;

        ammoCount--;

        if (!isFiring)
        {
            isFiring = true;

            fullClipSource.Stop();
            fullClipSource.time = 0f;
            fullClipSource.Play();

            InvokeRepeating(nameof(LoopIfNeeded), 0.1f, 0.05f);
        }

        Vector3 fireDir = mainCamera ? mainCamera.transform.forward : transform.forward;
        Vector3 fireOrigin = firePoint ? firePoint.position : transform.position;

        tracerFX?.PlayTracer(fireDir, fireOrigin);

        if (Physics.Raycast(fireOrigin, fireDir, out RaycastHit hit, fireRange))
        {
            impactFX?.PlayImpact(hit.point, hit.normal, hit.collider.tag);
        }
    }

    public void Stop()
    {
        if (isFiring)
        {
            isFiring = false;
            CancelInvoke(nameof(LoopIfNeeded));

            if (fullClipSource.isPlaying && fullClipSource.time < tailStartTime)
            {
                fullClipSource.time = tailStartTime;
            }
        }
    }

    public bool CanFire()
    {
        return ammoCount > 0;
    }

    public float GetFireRate()
    {
        return visualFireRate;
    }

    public int GetRemainingAmmo()
    {
        return ammoCount;
    }

    private void LoopIfNeeded()
    {
        if (!isFiring) return;

        if (fullClipSource.time >= tailStartTime)
        {
            fullClipSource.time = loopStartTime;
        }
    }
}
