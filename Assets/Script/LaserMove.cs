using UnityEngine;

public class LaserMove : MonoBehaviour
{
    public float speed = 50f;         // 激光前进速度
    public float lifetime = 3f;       // 自动销毁时间

    void Start()
    {
        // 一定时间后自动销毁
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        // 沿前方向移动
        transform.position += transform.forward * speed * Time.deltaTime;
    }

}
