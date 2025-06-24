using UnityEngine;
using System.Collections;

/// <summary>
/// USS_Shield：通用武器系统中的防御盾牌模块，实现 IWeaponContinuous 接口。
/// 可通过 WeaponControl 控制举盾和收起，并进行位置插值移动。
/// 实际格挡逻辑与受击判定尚未实现，预留接口供未来扩展。
/// </summary>
[RequireComponent(typeof(Transform))]
public class USS_Shield : MonoBehaviour, IWeapon, IWeaponContinuous
{
    [Header("盾牌举起位置")]
    [Tooltip("举起盾牌时目标位置的空物体Transform")]
    [SerializeField] private Transform shieldRaisedTransform;

    [Tooltip("实际进行移动的盾牌视觉对象（如有子物体）")]
    [SerializeField] private Transform shieldVisualTransform;

    [Header("盾牌移动参数")]
    [Tooltip("移动模式：匀速或平滑插值")]
    [SerializeField] private ShieldMoveMode moveMode = ShieldMoveMode.SmoothLerp;

    [Tooltip("举盾/收回时的移动速度")]
    [SerializeField] private float moveSpeed = 5f;

    [Tooltip("插值移动的最小距离阈值，避免抖动")]
    [SerializeField] private float moveThreshold = 0.01f;

    [Header("盾牌状态输出")]
    [Tooltip("当前是否处于举盾状态")]
    [ReadOnlyInInspector] public bool isShieldActive = false;

    private Vector3 loweredPosition;
    private Quaternion loweredRotation;

    private bool isMoving = false;
    private Coroutine moveRoutine;

    public enum ShieldMoveMode { ConstantSpeed, SmoothLerp }

    private void Awake()
    {
        if (shieldVisualTransform == null)
            shieldVisualTransform = this.transform;

        loweredPosition = shieldVisualTransform.localPosition;
        loweredRotation = shieldVisualTransform.localRotation;
    }

    public void Fire()
    {
        StartFiring();
    }

    public void Stop()
    {
        StopFiring();
    }

    public void StartFiring()
    {
        if (isShieldActive) return;
        isShieldActive = true;
        StartMove(shieldRaisedTransform.localPosition, shieldRaisedTransform.localRotation);
    }

    public void StopFiring()
    {
        if (!isShieldActive) return;
        isShieldActive = false;
        StartMove(loweredPosition, loweredRotation);
    }

    private void StartMove(Vector3 targetPos, Quaternion targetRot)
    {
        if (moveRoutine != null) StopCoroutine(moveRoutine);
        moveRoutine = StartCoroutine(MoveToPosition(targetPos, targetRot));
    }

    private IEnumerator MoveToPosition(Vector3 targetPosition, Quaternion targetRotation)
    {
        isMoving = true;
        while (true)
        {
            switch (moveMode)
            {
                case ShieldMoveMode.ConstantSpeed:
                    shieldVisualTransform.localPosition = Vector3.MoveTowards(shieldVisualTransform.localPosition, targetPosition, moveSpeed * Time.deltaTime);
                    shieldVisualTransform.localRotation = Quaternion.RotateTowards(shieldVisualTransform.localRotation, targetRotation, moveSpeed * 100f * Time.deltaTime);
                    break;
                case ShieldMoveMode.SmoothLerp:
                    shieldVisualTransform.localPosition = Vector3.Lerp(shieldVisualTransform.localPosition, targetPosition, moveSpeed * Time.deltaTime);
                    shieldVisualTransform.localRotation = Quaternion.Slerp(shieldVisualTransform.localRotation, targetRotation, moveSpeed * Time.deltaTime);
                    break;
            }

            float posDiff = Vector3.Distance(shieldVisualTransform.localPosition, targetPosition);
            float rotDiff = Quaternion.Angle(shieldVisualTransform.localRotation, targetRotation);

            if (posDiff < moveThreshold && rotDiff < 1f)
                break;

            yield return null;
        }

        shieldVisualTransform.localPosition = targetPosition;
        shieldVisualTransform.localRotation = targetRotation;
        isMoving = false;
    }

    public bool CanFire()
    {
        return true; // 始终允许举盾
    }

    public float GetFireRate()
    {
        return 9999f; // 极高射速，代表持续性触发
    }

    public FireMode GetFireMode()
    {
        return FireMode.Auto; // 持续触发型武器
    }

    // 预留接口：将来供主角受击系统调用，判断是否可格挡伤害
    public bool TryBlockIncomingDamage(object damageInfo)
    {
        if (!isShieldActive) return false;

        // TODO: 可添加角度判断、能量判断、穿透判断等
        return true;
    }
}

// Editor 扩展用：使只读字段在Inspector中显示
public class ReadOnlyInInspectorAttribute : PropertyAttribute { }
