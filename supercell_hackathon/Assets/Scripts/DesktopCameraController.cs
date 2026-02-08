using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// FPS camera with collision and gravity for desktop testing.
/// Uses CharacterController so you can't walk through walls or float.
/// WASD = move, Mouse = look, Space = jump, Escape = free cursor.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class DesktopCameraController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float lookSpeed = 0.1f;
    public float gravity = -20f;
    public float jumpForce = 6f;

    private float rotX = 0f;
    private float rotY = 0f;
    private CharacterController cc;
    private float verticalVelocity = 0f;

    void Start()
    {
        cc = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        rotY = transform.eulerAngles.y;
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        var mouse = Mouse.current;
        if (keyboard == null || mouse == null) return;

        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (mouse.leftButton.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // Mouse look
        Vector2 mouseDelta = mouse.delta.ReadValue();
        rotY += mouseDelta.x * lookSpeed;
        rotX -= mouseDelta.y * lookSpeed;
        rotX = Mathf.Clamp(rotX, -90f, 90f);
        transform.rotation = Quaternion.Euler(rotX, rotY, 0f);

        // WASD movement (horizontal only — gravity handles vertical)
        Vector3 forward = transform.forward;
        Vector3 right = transform.right;
        // Flatten to horizontal plane so looking down doesn't push you into the floor
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 move = Vector3.zero;
        if (keyboard.wKey.isPressed) move += forward;
        if (keyboard.sKey.isPressed) move -= forward;
        if (keyboard.dKey.isPressed) move += right;
        if (keyboard.aKey.isPressed) move -= right;
        move = move.normalized * moveSpeed;

        // Gravity + Jump
        if (cc.isGrounded)
        {
            verticalVelocity = -2f; // Small downward force to stay grounded
            if (keyboard.spaceKey.wasPressedThisFrame)
            {
                verticalVelocity = jumpForce;
            }
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }

        move.y = verticalVelocity;

        // CharacterController.Move handles collision for us
        cc.Move(move * Time.deltaTime);
    }
}
