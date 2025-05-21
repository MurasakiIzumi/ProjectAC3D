using Unity.Mathematics;
using UnityEngine;
using UnityEngine.EventSystems;
using static UnityEditor.Searcher.SearcherWindow.Alignment;

public class PlayerMove : MonoBehaviour
{
    public float MaxSpeed;
    public float rotatespeed;
    [SerializeField] private int gear;
    [SerializeField] private float moveSpeed;
    [SerializeField] private float targetSpeed;
    [SerializeField] private float upSpeed;
    [SerializeField] private float downSpeed;

    void Start()
    {
        gear = 0;
        moveSpeed = 0;
        targetSpeed = 0;
        upSpeed = MaxSpeed / 5f;
        downSpeed = MaxSpeed / 2f;
    }

    void Update()
    {
        ChangeGear();
        ChangeTargetSpeed();
        ChangeSpeed();
        Move();
        Rotate();
    }

    private void ChangeGear()
    {
        if (Input.GetKeyDown(KeyCode.W))
        {
            gear++;
        }
        else if (Input.GetKeyDown(KeyCode.S))
        {
            gear--;
        }

        gear = Mathf.Max(-1, Mathf.Min(gear, 5));
    }

    private void ChangeTargetSpeed()
    {
        targetSpeed = gear * MaxSpeed / 5f;
    }

    private void ChangeSpeed()
    {
        if (targetSpeed > moveSpeed)
        {
            moveSpeed += upSpeed * Time.deltaTime;
        }
        else if (targetSpeed < moveSpeed)
        {
            moveSpeed -= downSpeed * Time.deltaTime;
        }

        if (Mathf.Abs(targetSpeed - moveSpeed) <= 0.1f)
        {
            moveSpeed = targetSpeed;

        }
    }

    private void Move()
    {
        transform.position += transform.forward * moveSpeed * Time.deltaTime;
    }

    private void Rotate()
    {
        int rotate_direction = 0;

        if (Input.GetKey(KeyCode.A))
        {
            rotate_direction = -1;
        }
        else if (Input.GetKey(KeyCode.D))
        {
            rotate_direction = 1;
        }

        transform.Rotate(0, rotate_direction * rotatespeed, 0);
    }
}
