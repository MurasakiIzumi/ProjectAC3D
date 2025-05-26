using UnityEngine;

public class PlayerMoveV2 : MonoBehaviour
{
    public enum MovementMode { Walk, Drive } // 步行与推进两种模式切换

    [Header("== 通用设置 ==")]
    public MovementMode currentMode = MovementMode.Drive; // 当前模式（默认推进）
    public Rigidbody rb; // 刚体组件，用于物理运动
    public Transform cameraTransform; // 应绑定为 CameraRig，用于获取Boost方向
    [SerializeField] private CameraRigContol cameraRigContol; // 控制镜头俯仰与旋转的组件

    [Header("== 步行设置 ==")]
    public float walkSpeed = 5f; // 步行时的移动速度
    public float walkTurnSpeed = 60f; // 步行时的旋转速度（坦克式转向）
    public float dodgeSpeed = 10f; // 步行状态下的后退闪避速度
    public float dodgeDuration = 0.3f; // 闪避持续时间
    public float dodgeEnergyCost = 20f; // 闪避消耗的能量值
    public float dodgeCooldown = 1.0f; // 闪避冷却时间

    [Header("== 推进设置 ==")]
    public float[] gearSpeeds = new float[5] { -10f, 0f, 10f, 20f, 30f }; // 每个档位对应的速度（-1~3档）
    public float[] energyRecoveryRatePerGear = new float[5] { 1f, 1.2f, 1f, 0.9f, 0.7f }; // 每档对应的能量恢复倍率
    public float accelerationUp = 10f; // 加速时速度插值速率
    public float accelerationDown = 25f; // 减速时速度插值速率
    public float turnSpeedDrive = 40f; // 推进模式下的旋转速度（坦克式）

    [Header("== Boost & 横滑 ==")]
    public float boostSpeed = 25f; // Boost 冲刺速度（只在触发时朝向镜头方向）
    public float boostEnergyPerSecond = 20f; // Boost 每秒消耗的能量值
    public float strafeSpeed = 15f; // 横滑速度（左右短距离闪避）
    public float strafeDuration = 0.3f; // 横滑持续时间
    public float strafeEnergyCost = 15f; // 横滑消耗的能量
    public float strafeCooldown = 1f; // 横滑冷却时间

    [Header("== 能量设置 ==")]
    public float maxEnergy = 100f; // 最大能量值
    public float energyRecoveryRate = 10f; // 能量基础恢复速度（每秒）
    public float recoveryDelay = 2f; // 最后使用后等待多少秒开始恢复能量

    private float currentEnergy;
    private float lastEnergyUseTime;
    private int gearIndex = 1;

    private bool isBoosting = false;
    private Vector3 boostDirection;

    private bool isStrafing = false;
    private float strafeTimer;
    private float strafeCooldownTimer;
    private Vector3 strafeDir;

    private bool isDodging = false;
    private float dodgeTimer;
    private float dodgeCooldownTimer;
    private Vector3 dodgeDir;

    // 初始化组件与能量状态
    void Start()
    {
        if (!rb) rb = GetComponent<Rigidbody>();
        currentEnergy = maxEnergy;
        boostDirection = transform.forward;
    }

    // 每帧更新：模式切换、输入处理、能量恢复
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            currentMode = currentMode == MovementMode.Walk ? MovementMode.Drive : MovementMode.Walk;
            if (cameraRigContol) cameraRigContol.enableLook = true;
        }

        HandleInput();
        RecoverEnergy();
    }

    // 物理帧更新：根据当前模式调用不同移动函数
    void FixedUpdate()
    {
        if (currentMode == MovementMode.Walk)
            HandleWalk();
        else
            HandleDrive();
    }

    // 输入处理：移动指令、闪避、Boost、横滑、换挡等
    void HandleInput()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        bool space = Input.GetKey(KeyCode.Space);

        // 横滑（两模式通用）
        if ((h > 0.5f || h < -0.5f) && space && !isStrafing && strafeCooldownTimer <= 0 && currentEnergy >= strafeEnergyCost)
        {
            isStrafing = true;
            strafeTimer = strafeDuration;
            strafeDir = h > 0 ? transform.right : -transform.right;
            currentEnergy -= strafeEnergyCost;
            lastEnergyUseTime = Time.time;
            strafeCooldownTimer = strafeCooldown;
            return;
        }

        if (currentMode == MovementMode.Walk)
        {
            // 后退闪避（仅空格，无方向）
            if (space && v == 0 && h == 0 && dodgeCooldownTimer <= 0 && currentEnergy >= dodgeEnergyCost)
            {
                isDodging = true;
                dodgeTimer = dodgeDuration;
                dodgeDir = -transform.forward;
                currentEnergy -= dodgeEnergyCost;
                lastEnergyUseTime = Time.time;
                dodgeCooldownTimer = dodgeCooldown;
            }
        }
        else if (currentMode == MovementMode.Drive)
        {
            // 换挡
            if (Input.GetKeyDown(KeyCode.W)) gearIndex = Mathf.Min(gearIndex + 1, gearSpeeds.Length - 1);
            if (Input.GetKeyDown(KeyCode.S)) gearIndex = Mathf.Max(gearIndex - 1, 0);

            // Boost 起冲（长按，无横向）
            if (space && !isStrafing && h == 0 && v == 0 && currentEnergy > 0)
            {
                boostDirection = cameraTransform.forward;
                boostDirection.y = 0;
                boostDirection.Normalize();
                isBoosting = true;
            }
            else if (!space || currentEnergy <= 0)
            {
                isBoosting = false;
            }
        }

        // 冷却时间更新
        strafeCooldownTimer = Mathf.Max(0, strafeCooldownTimer - Time.deltaTime);
        dodgeCooldownTimer = Mathf.Max(0, dodgeCooldownTimer - Time.deltaTime);
    }

    // 步行模式下的移动控制
    void HandleWalk()
    {
        if (isDodging)
        {
            dodgeTimer -= Time.fixedDeltaTime;
            if (dodgeTimer <= 0f) isDodging = false;
            rb.linearVelocity = dodgeDir * dodgeSpeed;
            return;
        }

        if (isStrafing)
        {
            strafeTimer -= Time.fixedDeltaTime;
            if (strafeTimer <= 0f) isStrafing = false;
            rb.linearVelocity = strafeDir * strafeSpeed;
            return;
        }

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        // 转向控制
        if (Mathf.Abs(h) > 0.1f)
        {
            Quaternion rot = Quaternion.Euler(0, h * walkTurnSpeed * Time.fixedDeltaTime, 0);
            rb.MoveRotation(rb.rotation * rot);
        }

        Vector3 targetVel = transform.forward * v * walkSpeed;
        rb.linearVelocity = Vector3.MoveTowards(rb.linearVelocity, targetVel, walkTurnSpeed * Time.fixedDeltaTime);
    }

    // 推进模式下的移动控制
    void HandleDrive()
    {
        if (isStrafing)
        {
            strafeTimer -= Time.fixedDeltaTime;
            if (strafeTimer <= 0f) isStrafing = false;
            rb.linearVelocity = strafeDir * strafeSpeed;
            return;
        }

        float h = Input.GetAxis("Horizontal");
        if (Mathf.Abs(h) > 0.1f)
        {
            Quaternion rot = Quaternion.Euler(0, h * turnSpeedDrive * Time.fixedDeltaTime, 0);
            rb.MoveRotation(rb.rotation * rot);
        }

        float baseSpeed = gearSpeeds[gearIndex];
        Vector3 targetDir = transform.forward;
        float targetSpeed = Mathf.Abs(baseSpeed);

        if (isBoosting)
        {
            float boostCost = boostEnergyPerSecond * Time.fixedDeltaTime;
            if (currentEnergy >= boostCost)
            {
                currentEnergy -= boostCost;
                lastEnergyUseTime = Time.time;

                // 触发 Boost 时赋值 boostDirection
                rb.linearVelocity = boostDirection * boostSpeed;
                return;
            }
            else
            {
                isBoosting = false;
            }
        }

        Vector3 targetVel = targetDir * targetSpeed;
        float accel = (targetVel.magnitude > rb.linearVelocity.magnitude) ? accelerationUp : accelerationDown;
        rb.linearVelocity = Vector3.MoveTowards(rb.linearVelocity, targetVel, accel * Time.fixedDeltaTime);
    }

    // 能量恢复逻辑：延迟后恢复，受档位倍率影响
    void RecoverEnergy()
    {
        if ((Time.time - lastEnergyUseTime) < recoveryDelay || isBoosting || isStrafing || isDodging)
            return;

        float rate = energyRecoveryRate * energyRecoveryRatePerGear[gearIndex];
        currentEnergy = Mathf.Min(maxEnergy, currentEnergy + rate * Time.deltaTime);
    }

    // 获取当前能量归一化值（用于UI）
    public float GetEnergyNormalized() => currentEnergy / maxEnergy;

    // 获取当前档位（逻辑值：-1~3）
    public int GetGear() => gearIndex - 1;
}