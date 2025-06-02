using UnityEngine;

public class FunnelControl : MonoBehaviour
{
    public enum FunnelState { Docked, LaunchJumpOut, LaunchToTarget, Attacking, ReturnToApproach, Returning }
    public FunnelState currentState = FunnelState.Docked;

    [Header("引用组件")]
    [Tooltip("机体本体 Transform，用于参考方向等")]
    public Transform hostMech;

    [Tooltip("浮游炮回收时的挂载点 Transform（通常为导弹槽位置）")]
    public Transform dockPoint;

    [Tooltip("目标锁定系统，提供当前选中目标")]
    public EnemySensor enemySensor;

    [Tooltip("浮游炮激光发射点 Transform（可为炮口）")]
    public Transform firePoint;

    [Tooltip("激光预制体，用于射击表现")]
    public GameObject laserPrefab;

    [Header("飞行参数")]
    [Tooltip("浮游炮从挂点弹出时的速度（单位：米/秒）")]
    public float launchSpeed = 10f;

    [Tooltip("浮游炮回收时的速度（单位：米/秒）")]
    public float returnSpeed = 15f;

    [Tooltip("环绕敌人的球形半径（浮游炮保持在该距离内飞行）")]
    public float orbitRadius = 4f;

    [Tooltip("浮游炮环绕路径每次变换的时间间隔（秒）")]
    public float orbitChangeInterval = 0.5f;

    [Header("轨道高度设置")]
    [Tooltip("发射时浮游炮正面方向上弹出的距离")]
    public float launchRiseHeight = 2f;

    [Tooltip("回收时浮游炮先飞到挂载点正上方的距离")]
    public float returnApproachHeight = 2f;

    [Header("攻击设置")]
    [Tooltip("浮游炮每轮攻击的持续时间（秒）")]
    public float attackDuration = 5f;

    [Tooltip("激光发射间隔时间（秒）")]
    public float shootInterval = 0.5f;

    [Tooltip("浮游炮攻击悬停时间（秒）")]
    public float pauseBeforeShoot = 0.3f;

    [Header("特效")]
    [Tooltip("浮游炮飞行时的尾焰粒子系统")]
    public ParticleSystem thrustVFX;

    // 私有变量
    private float attackTimer = 0f;
    private float shootCooldown = 0f;
    private float orbitLerpTime = 0f;

    private Transform lockedTarget = null;
    private Vector3 currentOrbitPos;
    private Vector3 nextOrbitPos;
    private float shootPauseTimer = 0f;
    private bool isPausedBeforeShoot = false;
    private Vector3 intermediatePos;

    void Start()
    {
        if (!hostMech || !dockPoint || !enemySensor)
            Debug.LogError("FunnelController 缺少关键引用！");

        transform.position = dockPoint.position;
        shootPauseTimer = pauseBeforeShoot;
    }

    void Update()
    {
        switch (currentState)
        {
            case FunnelState.Docked:
                FollowDockPoint();
                break;
            case FunnelState.LaunchJumpOut:
                LaunchJumpOut();
                break;
            case FunnelState.LaunchToTarget:
                LaunchToTarget();
                break;
            case FunnelState.Attacking:
                OrbitAndAttack();
                break;
            case FunnelState.ReturnToApproach:
                ReturnToApproach();
                break;
            case FunnelState.Returning:
                ReturnToDock();
                break;
        }
    }

    public void ActivateFunnel()
    {
        if (enemySensor.HasTarget)
        {
            GameObject currentTarget = enemySensor.GetSelectedEnemy();
            if (currentTarget == null) return;

            lockedTarget = currentTarget.transform;
            intermediatePos = transform.position + transform.forward * launchRiseHeight;
            nextOrbitPos = lockedTarget.position + Random.onUnitSphere * orbitRadius;

            currentState = FunnelState.LaunchJumpOut;
        }
    }

    private void FollowDockPoint()
    {
        transform.position = dockPoint.position;
        transform.rotation = dockPoint.rotation;
    }

    private void LaunchJumpOut()
    {
        EnableThrustVFX();

        transform.position = Vector3.MoveTowards(transform.position, intermediatePos, launchSpeed * Time.deltaTime);
        RotateTowardsMovement(intermediatePos);

        if (Vector3.Distance(transform.position, intermediatePos) < 0.1f)
        {
            currentState = FunnelState.LaunchToTarget;
        }
    }

    private void LaunchToTarget()
    {
        if (lockedTarget == null) { currentState = FunnelState.ReturnToApproach; return; }

        transform.position = Vector3.MoveTowards(transform.position, nextOrbitPos, launchSpeed * Time.deltaTime);
        RotateTowardsMovement(nextOrbitPos);

        if (Vector3.Distance(transform.position, nextOrbitPos) < 0.5f)
        {
            Vector3 targetPos = lockedTarget.position;
            currentOrbitPos = nextOrbitPos;
            nextOrbitPos = targetPos + Random.onUnitSphere * orbitRadius;

            orbitLerpTime = 0f;
            shootCooldown = 0f;
            attackTimer = attackDuration;

            currentState = FunnelState.Attacking;
        }
    }

    private void OrbitAndAttack()
    {
        if (lockedTarget == null) { currentState = FunnelState.ReturnToApproach; return; }
        Vector3 targetPos = lockedTarget.position;

        if (isPausedBeforeShoot)
        {
            shootPauseTimer -= Time.deltaTime;
            if (shootPauseTimer <= 0f)
            {
                isPausedBeforeShoot = false;
                currentOrbitPos = nextOrbitPos;
                nextOrbitPos = targetPos + Random.onUnitSphere * orbitRadius;
                orbitLerpTime = 0f;
            }
            return;
        }

        orbitLerpTime += Time.deltaTime / orbitChangeInterval;
        transform.position = Vector3.Lerp(currentOrbitPos, nextOrbitPos, orbitLerpTime);
        transform.LookAt(targetPos);

        if (orbitLerpTime >= 1f)
        {
            isPausedBeforeShoot = true;
            shootPauseTimer = pauseBeforeShoot;
            FireLaser(lockedTarget);
        }

        attackTimer -= Time.deltaTime;
        if (attackTimer <= 0f)
        {
            currentState = FunnelState.ReturnToApproach;
        }
    }

    private void ReturnToApproach()
    {
        intermediatePos = dockPoint.position + Vector3.up * returnApproachHeight;

        transform.position = Vector3.MoveTowards(transform.position, intermediatePos, returnSpeed * Time.deltaTime);
        RotateTowardsMovement(intermediatePos);

        if (Vector3.Distance(transform.position, intermediatePos) < 0.2f)
        {
            currentState = FunnelState.Returning;
        }
    }

    private void ReturnToDock()
    {
        DisableThrustVFX();

        transform.position = Vector3.MoveTowards(transform.position, dockPoint.position, returnSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, dockPoint.rotation, Time.deltaTime * 5f);

        if (Vector3.Distance(transform.position, dockPoint.position) < 0.1f)
        {
            lockedTarget = null; // 清除缓存，准备下次新目标
            currentState = FunnelState.Docked;
        }
    }

    private void FireLaser(Transform targetTransform)
    {
        if (laserPrefab && firePoint && targetTransform)
        {
            GameObject laser = Instantiate(laserPrefab, firePoint.position, Quaternion.identity);
            laser.transform.LookAt(targetTransform.position);
        }
    }

    private void RotateTowardsMovement(Vector3 moveTarget, float speed = 10f)
    {
        Vector3 dir = (moveTarget - transform.position).normalized;
        if (dir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * speed);
        }
    }

    private void EnableThrustVFX()
    {
        if (thrustVFX && !thrustVFX.isPlaying)
            thrustVFX.Play();
    }

    private void DisableThrustVFX()
    {
        if (thrustVFX && thrustVFX.isPlaying)
            thrustVFX.Stop();
    }
}
