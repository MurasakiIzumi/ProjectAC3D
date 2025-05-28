using UnityEngine;

public class FunnelControl : MonoBehaviour
{
    public enum FunnelState { Docked, Launching, Attacking, Returning }
    public FunnelState currentState = FunnelState.Docked;

    [Header("References")]
    public Transform hostMech;             // 机体主体
    public Transform dockPoint;            // 悬挂点/回收点
    public EnemySensor enemySensor;        // 用于获取锁定敌人
    public Transform firePoint;            // 激光发射点
    public GameObject laserPrefab;         // 激光预制体

    [Header("Flight Settings")]
    public float launchSpeed = 10f;        // 发射阶段速度
    public float returnSpeed = 15f;        // 回收阶段速度
    public float orbitRadius = 4f;         // 环绕半径
    public float orbitChangeInterval = 0.5f; // 每次跳跃的间隔时间

    [Header("Attack Settings")]
    public float attackDuration = 5f;      // 攻击持续时间
    public float shootInterval = 0.5f;     // 发射激光间隔

    // 私有变量
    private float attackTimer = 0f;
    private float shootCooldown = 0f;
    private float orbitLerpTime = 0f;

    private Transform target;
    private Vector3 currentOrbitPos;
    private Vector3 nextOrbitPos;
    private float shootPauseTimer = 0f;
    private bool isPausedBeforeShoot = false;

    void Start()
    {
        if (!hostMech || !dockPoint || !enemySensor)
            Debug.LogError("FunnelController 缺少关键引用！");

        transform.position = dockPoint.position;
    }

    void Update()
    {
        switch (currentState)
        {
            case FunnelState.Docked:
                FollowDockPoint();
                break;
            case FunnelState.Launching:
                LaunchToTarget();
                break;
            case FunnelState.Attacking:
                OrbitAndAttack();
                break;
            case FunnelState.Returning:
                ReturnToDock();
                break;
        }
    }

    // 外部调用以启动浮游炮（通常由玩家按键或技能触发）
    public void ActivateFunnel()
    {
        if (enemySensor.HasTarget)
        {
            target = enemySensor.GetSelectedEnemy().transform;
            currentState = FunnelState.Launching;
        }
    }

    // 状态：挂载中（保持位置）
    private void FollowDockPoint()
    {
        transform.position = dockPoint.position;
        transform.rotation = dockPoint.rotation;
    }

    // 状态：飞向敌人上方
    private void LaunchToTarget()
    {
        if (!target) { currentState = FunnelState.Returning; return; }

        Vector3 targetPos = target.position + Vector3.up * 3f;
        transform.position = Vector3.MoveTowards(transform.position, targetPos, launchSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetPos) < 0.5f)
        {
            // 环绕初始点与目标点初始化
            currentOrbitPos = transform.position;
            nextOrbitPos = target.position + Random.onUnitSphere * orbitRadius;
            attackTimer = attackDuration;
            shootCooldown = 0f;
            orbitLerpTime = 0f;
            currentState = FunnelState.Attacking;
        }
    }

    // 状态：围绕目标飞行并攻击
    private void OrbitAndAttack()
    {
        if (!target) { currentState = FunnelState.Returning; return; }

        if (isPausedBeforeShoot)
        {
            // 暂停期间保持位置不动
            shootPauseTimer -= Time.deltaTime;
            if (shootPauseTimer <= 0f)
            {
                // 恢复移动
                currentOrbitPos = nextOrbitPos;
                nextOrbitPos = target.position + Random.onUnitSphere * orbitRadius;
                orbitLerpTime = 0f;
                isPausedBeforeShoot = false;
            }
            return;
        }

        // 移动阶段
        orbitLerpTime += Time.deltaTime / orbitChangeInterval;
        transform.position = Vector3.Lerp(currentOrbitPos, nextOrbitPos, orbitLerpTime);
        transform.LookAt(target.position);

        if (orbitLerpTime >= 1f)
        {
            // 到达后暂停，并射击
            isPausedBeforeShoot = true;
            shootPauseTimer = 1f; // 停顿1秒
            FireLaser();
        }

        attackTimer -= Time.deltaTime;
        if (attackTimer <= 0f)
        {
            currentState = FunnelState.Returning;
        }
    }

    // 状态：回收回挂载点
    private void ReturnToDock()
    {
        transform.position = Vector3.MoveTowards(transform.position, dockPoint.position, returnSpeed * Time.deltaTime);
        transform.LookAt(dockPoint.position);

        if (Vector3.Distance(transform.position, dockPoint.position) < 0.2f)
        {
            currentState = FunnelState.Docked;
        }
    }

    // 激光射击逻辑
    private void FireLaser()
    {
        if (laserPrefab && firePoint && target)
        {
            GameObject laser = Instantiate(laserPrefab, firePoint.position, Quaternion.identity);
            laser.transform.LookAt(target.position);
            // 建议在 laserPrefab 上加入 AutoDestroy 或动画结束时销毁
        }
    }
}
