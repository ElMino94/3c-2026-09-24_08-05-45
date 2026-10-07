using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Références")]
    [SerializeField] private Transform cameraHolder;

    [Header("Déplacement")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;

    [Header("Courbe d'accélération")]
    [SerializeField] private AnimationCurve accelerationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private float accelerationTime = 0.25f;

    [Header("Saut")]
    [SerializeField] private float jumpHeight = 1.2f;
    [SerializeField] private float gravity = -20f;
    [Tooltip("Temps (en secondes) pendant lequel on peut encore sauter après avoir quitté le sol")]
    [SerializeField] private float coyoteTime = 0.12f;
    [Tooltip("Temps (en secondes) pendant lequel un appui sur Saut est mémorisé avant d'atterrir")]
    [SerializeField] private float jumpBufferTime = 0.15f;
    [Tooltip("Multiplicateur de gravité : X = avancement du saut (0 = décollage, 0.5 = sommet, 1 = chute), Y = multiplicateur")]
    [SerializeField]
    private AnimationCurve gravityCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.5f, 1f), new Keyframe(1f, 2f));

    [Header("Souris")]
    [SerializeField] private float lookSensitivity = 0.1f;
    [SerializeField] private float maxLookAngle = 85f;

    private CharacterController controller;
    private PlayerControls controls;

    private Vector2 moveInput;
    private Vector2 lookInput;
    private float pitch; 

    private float accelProgress;
    private Vector3 lastMoveDir;

    private float verticalVelocity;
    private float coyoteCounter;
    private float bufferCounter;

    private float JumpVelocity => Mathf.Sqrt(jumpHeight * -2f * gravity);

    private void Awake() {

        controller = GetComponent<CharacterController>();
        controls = new PlayerControls();

    }

    private void OnEnable() {

        controls.Player.Enable();

    }

    private void OnDisable() {

        controls.Player.Disable();

    }

    private void Start() {

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

    }

    private void Update() {

        moveInput = controls.Player.Move.ReadValue<Vector2>();
        lookInput = controls.Player.Look.ReadValue<Vector2>();

        HandleLook();

        Vector3 horizontalVelocity = GetHorizontalVelocity();
        HandleJump();

        Vector3 velocity = horizontalVelocity + Vector3.up * verticalVelocity;
        controller.Move(velocity * Time.deltaTime);

        if ((controller.collisionFlags & CollisionFlags.Above) != 0 && verticalVelocity > 0f) {

            verticalVelocity = 0f;

        }

    }

    private void HandleLook() {

        transform.Rotate(Vector3.up * lookInput.x * lookSensitivity);

        pitch -= lookInput.y * lookSensitivity;
        pitch = Mathf.Clamp(pitch, -maxLookAngle, maxLookAngle);
        cameraHolder.localRotation = Quaternion.Euler(pitch, 0f, 0f);

    }

    private Vector3 GetHorizontalVelocity() {

        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        move = Vector3.ClampMagnitude(move, 1f);

        bool hasInput = moveInput.sqrMagnitude > 0.01f;

        if (hasInput)
            lastMoveDir = move;

        accelProgress += (hasInput ? 1f : -1f) * Time.deltaTime / accelerationTime;
        accelProgress = Mathf.Clamp01(accelProgress);

        float curveFactor = accelerationCurve.Evaluate(accelProgress);
        float speed = controls.Player.Sprint.IsPressed() ? sprintSpeed : walkSpeed;

        return lastMoveDir * (speed * curveFactor);

    }

    private void HandleJump() {

        if (controller.isGrounded) {

            coyoteCounter = coyoteTime;

            if (verticalVelocity < 0f)
                verticalVelocity = -2f;

        }
        else {

            coyoteCounter -= Time.deltaTime;
        
        }

        if (controls.Player.Jump.WasPressedThisFrame())
            bufferCounter = jumpBufferTime;
        else
            bufferCounter -= Time.deltaTime;

        if (bufferCounter > 0f && coyoteCounter > 0f) {

            verticalVelocity = JumpVelocity;
            bufferCounter = 0f;
            coyoteCounter = 0f;

        }

        float t = Mathf.InverseLerp(JumpVelocity, -JumpVelocity, verticalVelocity);
        float gravityMultiplier = gravityCurve.Evaluate(t);

        verticalVelocity += gravity * gravityMultiplier * Time.deltaTime;

    }

}