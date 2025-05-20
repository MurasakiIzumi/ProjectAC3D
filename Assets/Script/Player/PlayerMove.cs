using UnityEngine;
using static UnityEditor.Searcher.SearcherWindow.Alignment;

public class PlayerMove : MonoBehaviour
{
    public float speed;
    private Rigidbody rb;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb=GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertiacl = Input.GetAxis("Vertical");

        Vector3 force = new Vector3(horizontal * speed, 0.0f, vertiacl * speed);

        rb.AddForce(force);
    }
}
