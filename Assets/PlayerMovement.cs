using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Ruch")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Rzut")]
    [SerializeField] private Key castKey = Key.Q;

    private Vector3 startPosition;

    void Start()
    {
        startPosition = transform.position;
    }

    void Update()
    {
        // --- RUCH (strzałki) ---
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

        // --- RZUT (Q) - mierzy odległość od punktu startowego ---
        if (Keyboard.current[castKey].wasPressedThisFrame)
        {
            float distance = Vector3.Distance(startPosition, transform.position);
            Debug.Log($"<color=#00BFFF>🎣 Rzut! Odległość: <b>{distance:F1}m</b></color>");
        }
    }
}
