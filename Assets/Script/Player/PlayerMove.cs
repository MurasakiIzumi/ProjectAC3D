using UnityEngine;
using static UnityEditor.Searcher.SearcherWindow.Alignment;

public class PlayerMove : MonoBehaviour
{
    public float movespeed;
    public float rotatespeed;
    private Rigidbody rb;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        PlayerMoveNormal();
        PlayerMoveDash();
        PlayerRotate();
    }

    private void PlayerMoveNormal()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertiacl = Input.GetAxis("Vertical");

        Vector3 move_velocity = new Vector3(horizontal, 0.0f, vertiacl);

        move_velocity = transform.TransformDirection(move_velocity);

        transform.position += move_velocity * movespeed * Time.deltaTime;
    }

    private void PlayerMoveDash()
    {
        if (Input.GetKey(KeyCode.Space))
        {
            float horizontal = Input.GetAxis("Horizontal");
            float vertiacl = Input.GetAxis("Vertical");

            Vector3 move_velocity = new Vector3(horizontal, 0.0f, vertiacl);

            move_velocity = transform.TransformDirection(move_velocity);

            transform.position += move_velocity * movespeed * 2f * Time.deltaTime;
        }

    }

    private void PlayerRotate()
    {
        int rotate_direction = 0;

        if (Input.GetKey(KeyCode.Q))
        {
            rotate_direction = -1;
        }
        else if (Input.GetKey(KeyCode.E))
        {
            rotate_direction = 1;
        }

        transform.Rotate(0, rotate_direction * rotatespeed, 0);
    }
}
