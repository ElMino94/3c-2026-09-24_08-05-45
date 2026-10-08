using UnityEngine;

// Contrôleur d'hélicoptère. Hérite de VehicleBase pour le système d'entrée/sortie déjà en place.
// Deux objets distincts sont utilisés : la racine (cet objet, qui gère position/cap pour de vrai)
// et un enfant visuel (bodyVisual) qui s'incline uniquement à l'écran, sans influencer le déplacement.
[RequireComponent(typeof(Rigidbody))]
public class HelicopterController : VehicleBase
{
    [Header("Références")]
    [Tooltip("L'enfant visuel qui s'incline (le corps de l'hélico). Laisse vide pour ne pas incliner visuellement.")]
    [SerializeField] private Transform bodyVisual;

    [Header("Inclinaison / Déplacement horizontal")]
    [SerializeField] private float maxTiltAngle = 20f;       // angle d'inclinaison maximum, en degrés
    [SerializeField] private float tiltSpeed = 90f;           // vitesse à laquelle l'inclinaison rejoint sa cible
    [SerializeField] private float horizontalAcceleration = 10f;
    [SerializeField] private float horizontalDrag = 3f;       // freinage naturel quand on relâche les touches
    [SerializeField] private float maxHorizontalSpeed = 15f;

    [Header("Altitude")]
    [SerializeField] private float verticalAcceleration = 6f;
    [SerializeField] private float verticalDrag = 4f;
    [SerializeField] private float maxVerticalSpeed = 8f;
    [SerializeField] private float minAltitude = 1f;

    [Header("Rotation sur lui-même (lacet)")]
    [SerializeField] private float yawSpeed = 60f; // degrés/seconde

    [Header("Caméra orbitale")]
    [SerializeField] private float orbitSensitivity = 0.2f;
    [SerializeField] private float minPitch = -40f;
    [SerializeField] private float maxPitch = 80f;
    [SerializeField] private float minZoom = 4f;
    [SerializeField] private float maxZoom = 20f;
    [SerializeField] private float zoomStep = 1.5f;
    [SerializeField] private float cameraSmoothSpeed = 10f;
    [SerializeField] private float cameraCollisionPadding = 0.3f;

    private PlayerControls controls;
    private Rigidbody rb;

    // État de l'inclinaison visuelle et du cap réel
    private float pitchAngle; // avant/arrière, purement visuel
    private float rollAngle;  // gauche/droite, purement visuel
    private float yawAngle;   // cap réel de l'hélico, utilisé pour le déplacement

    // Vitesses actuelles
    private Vector3 horizontalVelocity;
    private float verticalVelocity;

    // État de la caméra orbitale
    private float orbitYaw;
    private float orbitPitch;
    private float currentZoom;

    private void Awake() {

        controls = new PlayerControls();

        rb = GetComponent<Rigidbody>();
        rb.useGravity = false; // l'hélico plane, l'altitude est gérée entièrement par le script
        // Rotation physique bloquée sur X/Z : seul le lacet (Y) est piloté, pour éviter
        // qu'un choc ne fasse culbuter l'hélico de façon imprévisible.
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    private void OnEnable() => controls.Player.Enable();
    private void OnDisable() => controls.Player.Disable();

    private void Update()
    {
        // Rien ne se passe tant que personne ne pilote l'hélico
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

        // La caméra se met à jour après tout le reste, une fois l'hélico déjà déplacé
        UpdateOrbitCamera();
    }

    private void HandleTiltAndMovement()
    {
        Vector2 move = controls.Player.Move.ReadValue<Vector2>();

        // Avancer (Z, move.y > 0) penche le nez vers le BAS (angle positif en X).
        // Aller à droite (D, move.x > 0) penche l'hélico vers la droite.
        float targetPitch = move.y * maxTiltAngle;
        float targetRoll = move.x * maxTiltAngle;

        // On rejoint l'angle cible progressivement plutôt que de sauter dessus instantanément
        pitchAngle = Mathf.MoveTowards(pitchAngle, targetPitch, tiltSpeed * Time.deltaTime);
        rollAngle = Mathf.MoveTowards(rollAngle, targetRoll, tiltSpeed * Time.deltaTime);

        // Cette inclinaison est PUREMENT visuelle : elle ne touche qu'à l'enfant bodyVisual,
        // jamais à la racine qui gère le vrai déplacement.
        if (bodyVisual != null)
            bodyVisual.localRotation = Quaternion.Euler(pitchAngle, 0f, -rollAngle);

        // Le déplacement réel est calculé à part, indépendamment de l'inclinaison visuelle
        Vector3 localIntent = new Vector3(move.x, 0f, move.y);

        if (localIntent.sqrMagnitude > 0.01f)
        {
            // On réoriente l'intention de déplacement selon le cap ACTUEL de l'hélico :
            // avancer avance toujours dans le sens où il pointe, même après une rotation.
            Vector3 worldIntent = Quaternion.Euler(0f, yawAngle, 0f) * localIntent.normalized;
            horizontalVelocity += worldIntent * horizontalAcceleration * Time.deltaTime;
        }
        else
        {
            // Aucune touche tenue : on ralentit progressivement (freinage naturel)
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
            // Ni l'un ni l'autre (ou les deux en même temps) : on ralentit vers zéro
            verticalVelocity = Mathf.MoveTowards(verticalVelocity, 0f, verticalDrag * Time.deltaTime);

        verticalVelocity = Mathf.Clamp(verticalVelocity, -maxVerticalSpeed, maxVerticalSpeed);
    }

    private void HandleYaw()
    {
        bool yawRight = controls.Player.YawRightInput.IsPressed();
        bool yawLeft = controls.Player.YawLeftInput.IsPressed();

        // Combine les deux touches en une seule valeur : 1, -1, ou 0 si aucune/les deux
        float yawInput = (yawRight ? 1f : 0f) - (yawLeft ? 1f : 0f);
        yawAngle += yawInput * yawSpeed * Time.deltaTime;
    }

    private void ApplyMovement()
    {
        // Seule la rotation sur Y (le cap) est appliquée à la racine : c'est elle qui compte
        // pour la physique, contrairement à l'inclinaison visuelle gérée plus haut.
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
        // La souris oriente la caméra autour de l'hélico (indépendamment de son cap)
        Vector2 look = controls.Player.Look.ReadValue<Vector2>();
        orbitYaw += look.x * orbitSensitivity;
        orbitPitch -= look.y * orbitSensitivity;
        orbitPitch = Mathf.Clamp(orbitPitch, minPitch, maxPitch);

        // La molette ajuste le zoom par petits crans fixes, plutôt que proportionnellement
        // à la valeur brute du scroll (qui varie beaucoup selon les systèmes)
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
        Vector3 pivot = transform.position + Vector3.up * 1.5f; // point au-dessus de l'hélico
        Quaternion orbitRotation = Quaternion.Euler(orbitPitch, orbitYaw, 0f);
        Vector3 direction = orbitRotation * Vector3.back;

        float actualDistance = currentZoom;

        // On cherche un obstacle entre le pivot et la position voulue de la caméra
        RaycastHit[] hits = Physics.RaycastAll(pivot, direction, currentZoom);
        foreach (RaycastHit hit in hits)
        {
            // On ignore l'hélico lui-même et le joueur (sa capsule reste présente même invisible),
            // sinon la caméra se bloquerait contre ces objets au lieu des vrais murs.
            if (hit.collider.GetComponentInParent<VehicleBase>() == this)
                continue;
            if (hit.collider.CompareTag("Player"))
                continue;

            float dist = hit.distance - cameraCollisionPadding;
            if (dist < actualDistance)
                actualDistance = dist;
        }

        actualDistance = Mathf.Max(actualDistance, 0.5f); // jamais totalement collée
        position = pivot + direction * actualDistance;
        rotation = Quaternion.LookRotation(pivot - position);
    }

    // Version utilisée en continu pendant le vol : glisse en douceur vers la position voulue
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
        base.EnterVehicle(driver); // active IsOccupied et allume vehicleCamera

        // Repart toujours d'un état propre à chaque entrée dans l'hélico
        horizontalVelocity = Vector3.zero;
        verticalVelocity = 0f;
        pitchAngle = 0f;
        rollAngle = 0f;
        yawAngle = transform.eulerAngles.y; // synchronise le cap sur l'orientation actuelle

        orbitYaw = yawAngle;
        orbitPitch = 20f;
        currentZoom = (minZoom + maxZoom) * 0.5f;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        SnapCameraToOrbit(); // place la caméra tout de suite au bon endroit
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