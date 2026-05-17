using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Windows;

public class PlayerMovement : NetworkBehaviour
{
    [SerializeField] float speed = 5.0f;
    [SerializeField] Vector2 input = Vector2.zero;
    void Start()
    {
        if (IsOwner)
        {
            GetComponent<SpriteRenderer>().color = Color.red;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsOwner) return;

        float horizontal = UnityEngine.Input.GetAxis("Horizontal");
        float vertical = UnityEngine.Input.GetAxis("Vertical");

        input = new Vector2(horizontal, vertical);

        transform.Translate(input.normalized * speed * Time.deltaTime);
    }

    //public void OnMove(InputValue movementvalue)
    //{
    //    input = movementvalue.Get<Vector2>();
    //}
}
