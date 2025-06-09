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
    [Tooltip("主监视器摄像机（决定默认射击方向）")]
    public Camera mainCamera;

    [Tooltip("射击起始点（即炮口位置）")]
    public Transform firePoint;

    [Tooltip("是否启用自动交汇射线（目标聚焦模式）")]
    public bool useAutoConverge = false;

    [Tooltip("最大汇聚距离（当无命中目标时射向该距离）")]
    public float convergeDistance = 30f;

    [Tooltip("交会最小距离限制，避免角度过大（单位：米）")]
    public float minConvergeDistance = 5f;

    [Tooltip("忽略射线命中的Layer（如Player、Cockpit）")]
    public LayerMask raycastIgnoreLayers;

    [Tooltip("无命中目标时是否取消交会（例如面向天空时改为平行射击）")]
    public bool disableConvergeWhenSky = true;

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

    [Tooltip("完整音频（包含开头、循环段、尾部）")]
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

        Vector3 fireOrigin = firePoint ? firePoint.position : transform.position;
        Vector3 fireDir = mainCamera ? mainCamera.transform.forward : transform.forward;

        if (useAutoConverge && mainCamera)
        {
            Ray camRay = new Ray(mainCamera.transform.position, mainCamera.transform.forward);
            Vector3 convergePoint;

            RaycastHit camHit;
            if (Physics.Raycast(camRay, out camHit, fireRange, ~raycastIgnoreLayers))
            {
                // 若目标太近，使用最小交会距离避免交叉过大
                float actualDistance = Vector3.Distance(camRay.origin, camHit.point);
                float usedDistance = Mathf.Max(actualDistance, minConvergeDistance);
                convergePoint = camRay.origin + camRay.direction * usedDistance;
            }
            else
            {
                // 没有命中任何目标（如看向天空）
                if (disableConvergeWhenSky)
                {
                    fireDir = mainCamera.transform.forward;
                    goto SKIP_CONVERGE;
                }

                // 正常使用预设交会距离
                convergePoint = camRay.origin + camRay.direction * convergeDistance;
            }

            fireDir = (convergePoint - fireOrigin).normalized;

        SKIP_CONVERGE:;
        }

        tracerFX?.PlayTracer(fireDir, fireOrigin);

        if (Physics.Raycast(fireOrigin, fireDir, out RaycastHit hit, fireRange, ~raycastIgnoreLayers))
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
