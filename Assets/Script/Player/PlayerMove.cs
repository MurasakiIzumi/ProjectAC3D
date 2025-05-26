using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    public enum MovementMode { Walk, Drive } // 移动模式（步行 / 推进）

    // == 通用设置 ==
    [Header("== 通用设置 ==")]
    public MovementMode currentMode = MovementMode.Drive; // 当前移动模式
    public Rigidbody rb; // 刚体组件
    public Transform cameraTransform; // 摄像机Transform，用于判断视角方向

    // == 步行设置 ==
    [Header("== 步行设置 ==")]
    public float walkSpeed = 5f; // 步行速度
    public float walkAcceleration = 20f; // 步行加速度
    public float dodgeSpeed = 15f; // 闪避速度
    public float dodgeDuration = 0.3f; // 闪避持续时间
    public float dodgeEnergyCost = 20f; // 闪避能量消耗
    public float dodgeCooldown = 1.0f; // 闪避冷却时间

    // == 推进模式设置 ==
    [Header("== 推进模式设置 ==")]
    public float[] gearSpeeds = new float[5] { -10f, 0f, 10f, 20f, 30f }; // 档位对应的速度，-1档到3档
    public float[] energyRecoveryRatePerGear = new float[5] { 1f, 1.2f, 1f, 0.9f, 0.7f }; // 各档能量恢复倍率
    public float accelerationUp = 10f; // 加速用插值速率
    public float accelerationDown = 25f; // 减速用插值速率
    public float glideFriction = 0.98f; // 空挡滑行时的摩擦系数
    public float dashBonusSpeed = 20f; // 冲刺额外速度
    public float dashDuration = 0.2f; // 冲刺持续时间
    public float dashEnergyCost = 15f; // 冲刺消耗能量
    public float boostBonus = 15f; // Boost 状态额外速度
    public float boostEnergyPerSecond = 20f; // Boost 每秒消耗能量
    public float strafeSpeed = 12f; // 横滑速度
    public float strafeDuration = 0.25f; // 横滑持续时间
    public float strafeEnergyCost = 15f; // 横滑能量消耗
    public float strafeCooldown = 1.0f; // 横滑冷却时间

    // == Boost 方向控制设定 ==
    [Header("== Boost方向渐变设定 ==")]
    public float angleThresholdForBoostRedirect = 30f; // Boost时若机体与视角夹角大于此值，将调整方向
    public float boostTurnSpeed = 5f; // Boost 状态下的转向速度（用于方向渐变）

    // == 能量系统 ==
    [Header("== 能量系统 ==")]
    public float maxEnergy = 100f; // 最大能量值
    public float energyRecoveryRate = 10f; // 基础能量恢复速度
    public float recoveryDelay = 2f; // 能量恢复延迟时间（最后消耗后等待）

    // == 内部状态管理 ==
    private float currentEnergy; // 当前能量值
    private float lastEnergyUseTime = -999f; // 上一次使用能量的时间戳
    private int gearIndex = 1; // 当前档位索引（默认空档）

    private bool isBoosting = false; // 当前是否处于 Boost 状态
    private bool isDashing = false; // 当前是否处于 Dash 冲刺状态
    private float dashTimer = 0f; // Dash 冲刺计时器

    private bool isDodging = false; // 当前是否处于闪避状态（步行模式）
    private float dodgeTimer = 0f; // 闪避计时器
    private float dodgeCooldownTimer = 0f; // 闪避冷却计时器
    private Vector3 dodgeDirection; // 闪避方向

    private bool isStrafing = false; // 当前是否处于横滑状态（推进模式）
    private float strafeTimer = 0f; // 横滑计时器
    private float strafeCooldownTimer = 0f; // 横滑冷却计时器
    private Vector3 strafeDirection; // 横滑方向

    private Vector3 lockedForward; // 推进模式中锁定的前进方向
    private bool forwardLocked = false; // 是否已锁定前进方向

    [SerializeField] private CameraRigContol cameraRigContol;

    // 初始化方法
    void Start()
    {
        if (!rb) rb = GetComponent<Rigidbody>();
        currentEnergy = maxEnergy;
        lockedForward = transform.forward;
    }

    // 每帧更新：处理输入与能量恢复逻辑
    void Update()
    {
        // 模式切换（步行 ↔ 推进）
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            currentMode = currentMode == MovementMode.Walk ? MovementMode.Drive : MovementMode.Walk;

            // 控制镜头是否启用自由视角
            if (cameraRigContol != null)
                cameraRigContol.enableLook = currentMode == MovementMode.Drive;
        }

        HandleInput();
        RecoverEnergy();
    }

    // 固定更新：处理物理逻辑（移动 / 推进）
    void FixedUpdate()
    {
        HandleRotation();

        if (currentMode == MovementMode.Walk)
            HandleWalkMode();
        else
            HandleDriveMode();
    }

    // 处理玩家输入（档位切换、闪避、Boost/Dash横滑等）
    void HandleInput()
    {
        if (currentMode == MovementMode.Walk)
        {
            // 步行状态下，按下空格执行闪避
            if (Input.GetKeyDown(KeyCode.Space) && dodgeCooldownTimer <= 0 && currentEnergy >= dodgeEnergyCost)
            {
                isDodging = true;
                dodgeTimer = dodgeDuration;
                dodgeDirection = transform.TransformDirection(new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical")).normalized);
                currentEnergy -= dodgeEnergyCost;
                lastEnergyUseTime = Time.time;
                dodgeCooldownTimer = dodgeCooldown;
            }
        }
        else // 推进模式
        {
            // 挡位切换逻辑
            if (Input.GetKeyDown(KeyCode.W))
            {
                gearIndex = Mathf.Min(gearIndex + 1, gearSpeeds.Length - 1);
                if (gearIndex != 1 && !forwardLocked)
                {
                    lockedForward = transform.forward;
                    forwardLocked = true;
                }
            }
            if (Input.GetKeyDown(KeyCode.S))
            {
                gearIndex = Mathf.Max(gearIndex - 1, 0);
                if (gearIndex == 1) forwardLocked = false;
            }

            // 横滑触发：方向 + B（空格）
            bool left = Input.GetAxis("Horizontal") < -0.5f;
            bool right = Input.GetAxis("Horizontal") > 0.5f;

            if ((left || right) && Input.GetKey(KeyCode.Space) && !isStrafing && strafeCooldownTimer <= 0 && currentEnergy >= strafeEnergyCost)
            {
                isStrafing = true;
                strafeTimer = strafeDuration;
                strafeDirection = right ? transform.right : -transform.right;
                currentEnergy -= strafeEnergyCost;
                lastEnergyUseTime = Time.time;
                strafeCooldownTimer = strafeCooldown;
            }
            else if (Input.GetKeyDown(KeyCode.Space) && !isDashing && currentEnergy >= dashEnergyCost)
            {
                // 冲刺 Dash
                isDashing = true;
                dashTimer = dashDuration;
                currentEnergy -= dashEnergyCost;
                lastEnergyUseTime = Time.time;
            }

            // Boost 长按（非冲刺/横滑中）
            isBoosting = Input.GetKey(KeyCode.Space) && !isDashing && !isStrafing && currentEnergy > 0;
        }

        if (gearIndex != 1 && !forwardLocked)
        {
            lockedForward = transform.forward;
            forwardLocked = true;
        }

        // 冷却时间更新
        dodgeCooldownTimer = Mathf.Max(0, dodgeCooldownTimer - Time.deltaTime);
        strafeCooldownTimer = Mathf.Max(0, strafeCooldownTimer - Time.deltaTime);
    }

    // 步行模式：常规移动 + 闪避控制
    void HandleWalkMode()
    {
        if (isDodging)
        {
            dodgeTimer -= Time.fixedDeltaTime;
            if (dodgeTimer <= 0f) isDodging = false;
            rb.linearVelocity = dodgeDirection * dodgeSpeed;
            return;
        }

        Vector3 input = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
        Vector3 move = transform.TransformDirection(input.normalized) * walkSpeed;
        rb.linearVelocity = Vector3.MoveTowards(rb.linearVelocity, move, walkAcceleration * Time.fixedDeltaTime);
    }

    // 推进模式：根据档位、Boost、冲刺、横滑状态推进机体
    void HandleDriveMode()
    {
        if (isStrafing)
        {
            strafeTimer -= Time.fixedDeltaTime;
            if (strafeTimer <= 0f) isStrafing = false;
            rb.linearVelocity = strafeDirection * strafeSpeed;
            return;
        }

        Vector3 direction = forwardLocked ? lockedForward : transform.forward;
        float baseSpeed = gearSpeeds[gearIndex];
        float targetSpeed = Mathf.Abs(baseSpeed);
        Vector3 targetDir = baseSpeed >= 0 ? direction : -direction;

        // 当前速度与方向相反时立即停止
        if (Vector3.Dot(rb.linearVelocity, targetDir) < 0f)
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }

        Vector3 targetVelocity = targetDir * targetSpeed;

        if (isDashing)
        {
            dashTimer -= Time.fixedDeltaTime;
            if (dashTimer <= 0f) isDashing = false;
            targetVelocity += targetDir * dashBonusSpeed;
        }
        else if (isBoosting)
        {
            // Boost 状态视角导向判断
            Vector3 camForward = cameraTransform.forward;
            camForward.y = 0;
            camForward.Normalize();
            float angle = Vector3.Angle(transform.forward, camForward);

            if (angle > angleThresholdForBoostRedirect)
            {
                Vector3 newDir = Vector3.Slerp(transform.forward, camForward, boostTurnSpeed * Time.fixedDeltaTime);
                targetDir = newDir;
                lockedForward = newDir;
            }

            float boostCost = boostEnergyPerSecond * Time.fixedDeltaTime;
            if (currentEnergy >= boostCost)
            {
                currentEnergy -= boostCost;
                lastEnergyUseTime = Time.time;
                targetVelocity = targetDir * (targetSpeed + boostBonus);
            }
            else
            {
                isBoosting = false;
            }
        }

        // 应用插值速度变化（加速 / 减速）
        float accel = (targetVelocity.magnitude > rb.linearVelocity.magnitude) ? accelerationUp : accelerationDown;
        rb.linearVelocity = Vector3.MoveTowards(rb.linearVelocity, targetVelocity, accel * Time.fixedDeltaTime);
    }

    // 控制机体旋转：在步行、Boost、Dash 中根据鼠标/右摇杆旋转机体
    void HandleRotation()
    {
        if (currentMode == MovementMode.Walk || isBoosting || isDashing)
        {
            float mouseX = Input.GetAxis("Mouse X");
            if (Mathf.Abs(mouseX) > 0.01f)
            {
                Quaternion deltaRot = Quaternion.Euler(0, mouseX * 3f, 0);
                rb.MoveRotation(rb.rotation * deltaRot);

                if (isBoosting || isDashing)
                    lockedForward = transform.forward;
            }
        }
    }

    // 能量恢复逻辑：延迟恢复 + 按当前档位倍率调整
    void RecoverEnergy()
    {
        if ((Time.time - lastEnergyUseTime) < recoveryDelay || isBoosting || isDashing || isDodging || isStrafing)
            return;

        float recoveryRate = energyRecoveryRate * energyRecoveryRatePerGear[gearIndex];
        currentEnergy += recoveryRate * Time.deltaTime;
        currentEnergy = Mathf.Min(currentEnergy, maxEnergy);
    }

    // 获取当前能量的归一化值（用于UI）
    public float GetEnergyNormalized() => currentEnergy / maxEnergy;

    // 获取当前逻辑档位值（-1 ~ 3）
    public int GetGear() => gearIndex - 1;
}
