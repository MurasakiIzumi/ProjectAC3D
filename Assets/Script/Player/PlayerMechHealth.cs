using UnityEngine;

public class PlayerMechHealth : MonoBehaviour
{
    [Tooltip("头部血量")]
    public float HeadHP = 0f;

    [Tooltip("身体血量")]
    public float BodyHP = 0f;

    [Tooltip("双足血量")]
    public float FootHP = 0f;

    [Tooltip("右手血量")]
    public float RightHandHP = 0f;

    [Tooltip("左手血量")]
    public float LeftHandHP = 0f;

    [Tooltip("每个部位的最大血量")]
    public float MaxHp = 100f;

    private void Start()
    {
        HeadHP = MaxHp;
        BodyHP = MaxHp;
        FootHP = MaxHp;
        RightHandHP = MaxHp;
        LeftHandHP = MaxHp;
    }
}
