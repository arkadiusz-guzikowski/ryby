using UnityEngine;
using UnityEngine.InputSystem;

public class colision : MonoBehaviour
{
    [SerializeField] float moveSpeed = 5f;

    void Update()
    {
        Vector2 movement = Vector2.zero;

        if (Keyboard.current.upArrowKey.isPressed)
            movement.y = 1;
        else if (Keyboard.current.downArrowKey.isPressed)
            movement.y = -1;

        if (Keyboard.current.leftArrowKey.isPressed)
            movement.x = -1;
        else if (Keyboard.current.rightArrowKey.isPressed)
            movement.x = 1;

        transform.Translate(movement.normalized * moveSpeed * Time.deltaTime);
    }
}

