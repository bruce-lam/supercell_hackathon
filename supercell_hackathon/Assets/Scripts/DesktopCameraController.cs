using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Simple FPS camera with collision for desktop testing.
/// Uses CharacterController so you can't walk through walls.
/// WASD = move, Mouse = look, Space = up, Ctrl = down, Escape = free cursor.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class DesktopCameraController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float lookSpeed = 0.1f;

    private float rotX = 0f;
    private float rotY = 0f;
    private CharacterController cc;

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

        // WASD movement with collision
        Vector3 move = Vector3.zero;
        if (keyboard.wKey.isPressed) move += transform.forward;
        if (keyboard.sKey.isPressed) move -= transform.forward;
        if (keyboard.dKey.isPressed) move += transform.right;
        if (keyboard.aKey.isPressed) move -= transform.right;
        if (keyboard.spaceKey.isPressed) move += Vector3.up;
        if (keyboard.leftCtrlKey.isPressed) move += Vector3.down;

        // CharacterController.Move handles collision for us
        cc.Move(move.normalized * moveSpeed * Time.deltaTime);
    }
}
