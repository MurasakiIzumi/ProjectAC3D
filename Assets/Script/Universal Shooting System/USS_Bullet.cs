using UnityEngine;

public struct BulletHitInfo
{
    public float damage;
    public int penetrationLevel;
    public bool isEnergy;
    public bool canPenetrate;
    public float penetrationPower;
    public float penetrationDecay;
    public Vector3 hitPoint;
    public Vector3 hitNormal;
    public GameObject sourceWeapon;
}

[RequireComponent(typeof(Rigidbody))]
public class USS_Bullet : MonoBehaviour
{
    [Header("基础参数")]
    [Tooltip("子弹飞行速度（单位：米/秒）")]
    public float speed = 50f;

    [Tooltip("子弹生命周期（秒）")]
    public float lifeTime = 5f;

    [Tooltip("是否使用伪重力（避免Rigidbody自动重力导致速度干扰）")]
    public bool useGravity = false;

    [Tooltip("伪重力强度倍率（1为正常重力）")]
    public float gravityMultiplier = 1.0f;

    [Header("命中数据")]
    [Tooltip("纸面伤害（不代表最终伤害）")]
    public float damage = 10f;

    [Tooltip("穿甲能力等级（整数）")]
    public int penetrationLevel = 0;

    [Tooltip("是否为能量武器")]
    public bool isEnergy = false;

    [Tooltip("是否具有穿透能力")]
    public bool canPenetrate = false;

    [Tooltip("穿透力数值（越高代表能继续命中更多单位）")]
    public float penetrationPower = 0f;

    [Tooltip("每次穿透后减少的穿透力")]
    public float penetrationDecay = 1f;

    [Tooltip("来源武器物体（用于追踪命中来源）")]
    public GameObject sourceWeapon;

    [Header("命中特效设置")]
    [Tooltip("攻击有效命中特效Prefab")]
    public GameObject hitEffectPrefabValid;

    [Tooltip("攻击无效命中特效Prefab（被装甲挡住）")]
    public GameObject hitEffectPrefabInvalid;

    [Header("能量弹衰减设置")]
    [Tooltip("能量弹最低强度比例（如 0.4 表示最低保留 40% 威力)")]
    public float minEnergyRatio = 0.4f;

    private Rigidbody rb;
    private float timer;
    private Vector3 gravityDirection = Vector3.down;
    private bool isPenetrating = false;
    private Vector3 continueDirection;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false; // 始终关闭物理重力，用自定义伪重力控制
        rb.linearVelocity = transform.forward * speed;
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= lifeTime)
        {
            Destroy(gameObject);
        }
        if (isPenetrating)
        {
            transform.position += continueDirection * speed * Time.deltaTime;
        }
    }

    void FixedUpdate()
    {
        if (useGravity && rb != null && !rb.isKinematic)
        {
            Vector3 gravity = gravityDirection * Physics.gravity.magnitude * gravityMultiplier;
            rb.linearVelocity += gravity * Time.fixedDeltaTime;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        ContactPoint contact = collision.contacts[0];

        // 地面接触检测（假设地面为 Ground Layer）
        if (collision.gameObject.CompareTag("Ground"))
        {
            Destroy(gameObject);
            return;
        }

        BulletHitInfo hitInfo = new BulletHitInfo
        {
            damage = GetCurrentDamage(),
            penetrationLevel = GetCurrentPenetrationLevel(),
            isEnergy = this.isEnergy,
            canPenetrate = this.canPenetrate,
            penetrationPower = GetCurrentPenetrationPower(),
            penetrationDecay = this.penetrationDecay,
            hitPoint = contact.point,
            hitNormal = contact.normal,
            sourceWeapon = this.gameObject
        };

        var target = collision.gameObject.GetComponent<IDamageReceiver>();
        if (target != null)
        {
            target.ReceiveBulletHit(hitInfo);
        }
    }

    public void SpawnHitEffect(Vector3 position, Vector3 normal, bool valid)
    {
        GameObject prefab = valid ? hitEffectPrefabValid : hitEffectPrefabInvalid;
        if (prefab != null)
        {
            Quaternion rot = Quaternion.LookRotation(normal);
            Instantiate(prefab, position, rot);
        }
    }

    public void OnDeflect(Vector3 inDirection, Vector3 normal)
    {
        Vector3 reflectDir = Vector3.Reflect(inDirection.normalized, normal).normalized;
        rb.linearVelocity = reflectDir * speed;
        rb.angularVelocity = Vector3.zero;
        transform.forward = reflectDir;
    }

    public void OnPenetrateContinue(Vector3 inDirection)
    {
        isPenetrating = true;
        continueDirection = inDirection.normalized;

        rb.linearVelocity = continueDirection * speed;
        rb.angularVelocity = Vector3.zero;

        rb.isKinematic = true;
    }

    private float GetDecayRatio()
    {
        return BulletMath.GetEnergyDecayRatio(lifeTime, timer, minEnergyRatio);
    }

    private float GetCurrentDamage()
    {
        return isEnergy ? BulletMath.GetEnergyDamage(damage, GetDecayRatio()) : damage;
    }

    private int GetCurrentPenetrationLevel()
    {
        return isEnergy ? BulletMath.GetEnergyPenetrationLevel(penetrationLevel, GetDecayRatio()) : penetrationLevel;
    }

    private float GetCurrentPenetrationPower()
    {
        return isEnergy ? BulletMath.GetEnergyPenetrationPower(penetrationPower, GetDecayRatio()) : penetrationPower;
    }
}

public interface IDamageReceiver
{
    void ReceiveBulletHit(BulletHitInfo hitInfo);
}
