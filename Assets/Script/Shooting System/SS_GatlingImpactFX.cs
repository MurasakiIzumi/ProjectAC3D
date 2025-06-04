using UnityEngine;

public class SS_GatlingImpactFX : MonoBehaviour
{
    [Tooltip("默认命中特效预制体")]
    public GameObject defaultImpactPrefab;

    [Tooltip("金属材质命中特效预制体")]
    public GameObject metalImpactPrefab;

    [Tooltip("护盾命中特效预制体")]
    public GameObject shieldImpactPrefab;

    [Tooltip("命中特效持续时间")]
    public float impactDuration = 0.4f;

    public void PlayImpact(Vector3 position, Vector3 normal, string tag)
    {
        GameObject fxPrefab = GetImpactPrefab(tag);
        if (fxPrefab == null) return;

        GameObject fx = Instantiate(fxPrefab, position, Quaternion.LookRotation(normal));
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
}
