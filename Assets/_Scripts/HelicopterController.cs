using UnityEngine;

// Contrôleur d'hélicoptère. Hérite de VehicleBase pour le système d'entrée/sortie déjà en place.
[RequireComponent(typeof(Rigidbody))]
public class HelicopterController : VehicleBase
{
    [Header("Références")]
    [Tooltip("L'enfant visuel qui s'incline (le corps de l'hélico). Laisse vide pour ne pas incliner visuellement.")]
    [SerializeField] private Transform bodyVisual;

    [Header("Inclinaison / Déplacement horizontal")]
    [SerializeField] private float maxTiltAngle = 20f;
    [SerializeField] private float tiltSpeed = 90f;
    [SerializeField] private float horizontalAcceleration = 10f;
    [SerializeField] private float horizontalDrag = 3f;
    [SerializeField] private float maxHorizontalSpeed = 15f;

    [Header("Altitude")]
    [SerializeField] private float verticalAcceleration = 6f;
    [SerializeField] private float verticalDrag = 4f;
    [SerializeField] private float maxVerticalSpeed = 8f;
    [Tooltip("Altitude minimale du centre de l'hélico, pour ne pas s'enfoncer dans le sol")]
    [SerializeField] private float minAltitude = 1f;

    [Header("Rotation sur lui-même (lacet)")]
    [SerializeField] private float yawSpeed = 60f;

    [Header("Caméra orbitale")]
    [SerializeField] private float orbitSensitivity = 0.2f;
    [SerializeField] private float minPitch = -40f;
    [SerializeField] private float maxPitch = 80f;
    [SerializeField] private float minZoom = 4f;
    [SerializeField] private float maxZoom = 20f;
    [Tooltip("Distance ajoutée/retirée à chaque cran de molette")]
    [SerializeField] private float zoomStep = 1.5f;
    [SerializeField] private float cameraSmoothSpeed = 10f;
    [Tooltip("Marge gardée entre la caméra et un obstacle qu'elle évite")]
    [SerializeField] private float cameraCollisionPadding = 0.3f;

    private PlayerControls controls;
    private Rigidbody rb;

    private float pitchAngle;
    private float rollAngle;
    private float yawAngle;
    private Vector3 horizontalVelocity;
    private float verticalVelocity;

    private float orbitYaw;
    private float orbitPitch;
    private float currentZoom;

    private void Awake()
    {
        controls = new PlayerControls();

        rb = GetComponent<Rigidbody>();
        rb.useGravity = false; // l'hélico plane, l'altitude est gérée entièrement par le script
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    private void OnEnable() => controls.Player.Enable();
    private void OnDisable() => controls.Player.Disable();

    private void Update()
    {
        if (!IsOccupied)
            return;

        HandleTiltAndMovement();
        HandleAltitude();
        HandleYaw();
        ApplyMovement();
        HandleCameraOrbitInput();
    }

    private void LateUpdate()
    {
        if (!IsOccupied)
            return;

        UpdateOrbitCamera();
    }

    private void HandleTiltAndMovement()
    {
        Vector2 move = controls.Player.Move.ReadValue<Vector2>();

        float targetPitch = -move.y * maxTiltAngle;
        float targetRoll = move.x * maxTiltAngle;

        pitchAngle = Mathf.MoveTowards(pitchAngle, targetPitch, tiltSpeed * Time.deltaTime);
        rollAngle = Mathf.MoveTowards(rollAngle, targetRoll, tiltSpeed * Time.deltaTime);

        if (bodyVisual != null)
            bodyVisual.localRotation = Quaternion.Euler(pitchAngle, 0f, -rollAngle);

        Vector3 localIntent = new Vector3(move.x, 0f, move.y);

        if (localIntent.sqrMagnitude > 0.01f)
        {
            Vector3 worldIntent = Quaternion.Euler(0f, yawAngle, 0f) * localIntent.normalized;
            horizontalVelocity += worldIntent * horizontalAcceleration * Time.deltaTime;
        }
        else
        {
            horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, Vector3.zero, horizontalDrag * Time.deltaTime);
        }

        horizontalVelocity = Vector3.ClampMagnitude(horizontalVelocity, maxHorizontalSpeed);
    }

    private void HandleAltitude()
    {
        bool ascend = controls.Player.AscendInput.IsPressed();
        bool descend = controls.Player.DescendInput.IsPressed();

        if (ascend && !descend)
            verticalVelocity += verticalAcceleration * Time.deltaTime;
        else if (descend && !ascend)
            verticalVelocity -= verticalAcceleration * Time.deltaTime;
        else
            verticalVelocity = Mathf.MoveTowards(verticalVelocity, 0f, verticalDrag * Time.deltaTime);

        verticalVelocity = Mathf.Clamp(verticalVelocity, -maxVerticalSpeed, maxVerticalSpeed);
    }

    private void HandleYaw()
    {
        bool yawRight = controls.Player.YawRightInput.IsPressed();
        bool yawLeft = controls.Player.YawLeftInput.IsPressed();

        float yawInput = (yawRight ? 1f : 0f) - (yawLeft ? 1f : 0f);
        yawAngle += yawInput * yawSpeed * Time.deltaTime;
    }

    private void ApplyMovement()
    {
        rb.MoveRotation(Quaternion.Euler(0f, yawAngle, 0f));

        Vector3 totalVelocity = horizontalVelocity + Vector3.up * verticalVelocity;

        // Avec la vraie physique, le Collider du sol bloque déjà normalement l'hélico.
        // On garde ce filet de sécurité au cas où l'altitude minimale ne serait pas respectée.
        if (transform.position.y <= minAltitude && totalVelocity.y < 0f)
        {
            totalVelocity.y = 0f;
            verticalVelocity = 0f;
        }

        rb.linearVelocity = totalVelocity;
    }

    private void HandleCameraOrbitInput()
    {
        Vector2 look = controls.Player.Look.ReadValue<Vector2>();
        orbitYaw += look.x * orbitSensitivity;
        orbitPitch -= look.y * orbitSensitivity;
        orbitPitch = Mathf.Clamp(orbitPitch, minPitch, maxPitch);

        float scrollY = controls.Player.Zoom.ReadValue<Vector2>().y;
        if (Mathf.Abs(scrollY) > 0.01f)
        {
            currentZoom -= Mathf.Sign(scrollY) * zoomStep;
            currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);
        }
    }

    // Calcule où la caméra orbitale DEVRAIT être en ce moment, évitement d'obstacles compris.
    // Partagé entre la version "glissée" (UpdateOrbitCamera) et la version instantanée (SnapCameraToOrbit).
    private void ComputeOrbitCameraTransform(out Vector3 position, out Quaternion rotation)
    {
        Vector3 pivot = transform.position + Vector3.up * 1.5f;
        Quaternion orbitRotation = Quaternion.Euler(orbitPitch, orbitYaw, 0f);
        Vector3 direction = orbitRotation * Vector3.back;

        float actualDistance = currentZoom;

        RaycastHit[] hits = Physics.RaycastAll(pivot, direction, currentZoom);
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.GetComponentInParent<VehicleBase>() == this)
                continue;
            if (hit.collider.CompareTag("Player"))
                continue;

            float dist = hit.distance - cameraCollisionPadding;
            if (dist < actualDistance)
                actualDistance = dist;
        }

        actualDistance = Mathf.Max(actualDistance, 0.5f);
        position = pivot + direction * actualDistance;
        rotation = Quaternion.LookRotation(pivot - position);
    }

    private void UpdateOrbitCamera()
    {
        if (vehicleCamera == null)
            return;

        ComputeOrbitCameraTransform(out Vector3 targetPosition, out Quaternion targetRotation);

        vehicleCamera.transform.position = Vector3.Lerp(
            vehicleCamera.transform.position, targetPosition, cameraSmoothSpeed * Time.deltaTime);
        vehicleCamera.transform.rotation = Quaternion.Slerp(
            vehicleCamera.transform.rotation, targetRotation, cameraSmoothSpeed * Time.deltaTime);
    }

    // Place la caméra directement à la bonne position, sans glisser depuis son ancien
    // emplacement : utile si l'hélico a été déplacé pendant qu'on n'était pas dedans.
    private void SnapCameraToOrbit()
    {
        if (vehicleCamera == null)
            return;

        ComputeOrbitCameraTransform(out Vector3 position, out Quaternion rotation);
        vehicleCamera.transform.position = position;
        vehicleCamera.transform.rotation = rotation;
    }

    public override void EnterVehicle(GameObject driver)
    {
        base.EnterVehicle(driver);

        horizontalVelocity = Vector3.zero;
        verticalVelocity = 0f;
        pitchAngle = 0f;
        rollAngle = 0f;
        yawAngle = transform.eulerAngles.y;

        orbitYaw = yawAngle;
        orbitPitch = 20f;
        currentZoom = (minZoom + maxZoom) * 0.5f;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        SnapCameraToOrbit();
    }

    public override void ExitVehicle(GameObject driver)
    {
        base.ExitVehicle(driver);
        horizontalVelocity = Vector3.zero;
        verticalVelocity = 0f;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }
}