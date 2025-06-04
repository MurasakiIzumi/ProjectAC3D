using UnityEngine;

public class SS_GatlingImpactFX : MonoBehaviour
{
    [Header("命中特效预制体")]
    [Tooltip("默认命中特效预制体")]
    public GameObject defaultImpactPrefab;

    [Tooltip("金属材质命中特效预制体")]
    public GameObject metalImpactPrefab;

    [Tooltip("护盾命中特效预制体")]
    public GameObject shieldImpactPrefab;

    [Header("命中音效")]
    [Tooltip("普通命中音效列表（非敌人）")]
    public AudioClip[] impactSounds;

    [Tooltip("敌人命中音效列表")]
    public AudioClip[] enemyHitSounds;

    [Tooltip("命中特效持续时间")]
    public float impactDuration = 0.4f;

    public void PlayImpact(Vector3 position, Vector3 normal, string tag)
    {
        GameObject fxPrefab = GetImpactPrefab(tag);
        if (fxPrefab == null) return;

        // 实例化命中特效
        GameObject fx = Instantiate(fxPrefab, position, Quaternion.LookRotation(normal));

        // 播放粒子
        ParticleSystem ps = fx.GetComponent<ParticleSystem>();
        if (ps) ps.Play();

        // 播放音效（从特效预制体的 AudioSource 发声）
        AudioSource audio = fx.GetComponent<AudioSource>();
        if (audio)
        {
            AudioClip clipToPlay = GetRandomImpactSound(tag);
            if (clipToPlay != null)
                audio.PlayOneShot(clipToPlay);
        }

        // 延迟销毁
        Destroy(fx, impactDuration);
    }

    private GameObject GetImpactPrefab(string tag)
    {
        switch (tag)
        {
            case "Enemy":
                return metalImpactPrefab;
            case "Shield":
                return shieldImpactPrefab;
            default:
                return defaultImpactPrefab;
        }
    }

    private AudioClip GetRandomImpactSound(string tag)
    {
        AudioClip[] source = (tag == "Enemy") ? enemyHitSounds : impactSounds;
        if (source != null && source.Length > 0)
        {
            int rand = Random.Range(0, source.Length);
            return source[rand];
        }
        return null;
    }
}
