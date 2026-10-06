using UnityEngine;

// Contrôleur de train. Suit une spline définie par des points de passage (waypoints),
// avec un risque de déraillement si la vitesse est trop grande pour le virage pris.
[RequireComponent(typeof(Rigidbody))]
public class TrainController : VehicleBase
{
    [Header("Références")]
    [Tooltip("L'enfant visuel qui penche en cas de déraillement. Laisse vide pour désactiver l'effet.")]
    [SerializeField] private Transform bodyVisual;

    [Header("Chemin (points de passage, dans l'ordre, en boucle)")]
    [Tooltip("Il en faut au moins 4 pour que la courbe fonctionne")]
    [SerializeField] private Transform[] waypoints;

    [Header("Vitesse")]
    [Tooltip("Très lente : c'est voulu, un train met du temps à démarrer")]
    [SerializeField] private float accelerationRate = 1.5f;
    [Tooltip("Lente aussi : un train a une très longue distance de freinage")]
    [SerializeField] private float brakeDeceleration = 2f;
    [SerializeField] private float coastingDeceleration = 0.3f;
    [SerializeField] private float maxSpeed = 12f;

    [Header("Risque de déraillement")]
    [Tooltip("Distance (en mètres) regardée en avant pour évaluer le virage à venir")]
    [SerializeField] private float curveLookahead = 5f;
    [Tooltip("En dessous de cette vitesse, aucun virage ne fait dérailler")]
    [SerializeField] private float minDerailSpeed = 6f;
    [Tooltip("Angle de virage (sur la distance regardée) à partir duquel la vitesse max autorisée tombe à Min Derail Speed")]
    [SerializeField] private float maxSafeTurnAngle = 60f;

    [Header("Caméra cinématique")]
    [Tooltip("4 positions prédéfinies (enfants du train), dans l'ordre d'alternance")]
    [SerializeField] private Transform[] cameraAnchors;
    [SerializeField] private float cameraHoldDuration = 6f;
    [Tooltip("Durée de la transition douce entre deux positions, en secondes")]
    [SerializeField] private float cameraTransitionDuration = 2.5f;

    private PlayerControls controls;
    private Rigidbody rb;

    private float currentSpeed;
    private int currentSegment;
    private float segmentT; // avancement de 0 à 1 sur le segment actuel
    private bool isDerailed;

    private int currentCameraAnchor;
    private float cameraTimer;
    private Vector3 transitionStartPosition;
    private float transitionProgress = 1f; // 1 = pas de transition en cours

    private void Awake()
    {
        controls = new PlayerControls();

        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true; // le train suit sa voie, pas la physique libre
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private void OnEnable() => controls.Player.Enable();
    private void OnDisable() => controls.Player.Disable();

    private void Update()
    {
        if (!IsOccupied)
            return;

        HandleSpeed();
        FollowPath();
    }

    private void LateUpdate()
    {
        if (!IsOccupied)
            return;

        UpdateCinematicCamera();
    }

    private void HandleSpeed()
    {
        if (isDerailed)
            return;

        Vector2 move = controls.Player.Move.ReadValue<Vector2>();
        bool accelerating = move.y > 0.1f;
        bool braking = move.y < -0.1f;

        if (accelerating)
            currentSpeed += accelerationRate * Time.deltaTime;
        else if (braking)
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, brakeDeceleration * Time.deltaTime);
        else
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, coastingDeceleration * Time.deltaTime);

        currentSpeed = Mathf.Clamp(currentSpeed, 0f, maxSpeed);
    }

    private void FollowPath()
    {
        if (waypoints == null || waypoints.Length < 4 || isDerailed)
            return;

        int n = waypoints.Length;

        float segmentLength = Vector3.Distance(
            waypoints[currentSegment % n].position,
            waypoints[(currentSegment + 1) % n].position);

        segmentT += currentSpeed * Time.deltaTime / Mathf.Max(segmentLength, 0.01f);

        while (segmentT >= 1f)
        {
            segmentT -= 1f;
            currentSegment = (currentSegment + 1) % n;
        }

        Vector3 p0 = waypoints[(currentSegment - 1 + n) % n].position;
        Vector3 p1 = waypoints[currentSegment % n].position;
        Vector3 p2 = waypoints[(currentSegment + 1) % n].position;
        Vector3 p3 = waypoints[(currentSegment + 2) % n].position;

        Vector3 position = CatmullRom(segmentT, p0, p1, p2, p3);

        // On regarde un petit peu plus loin pour orienter le train dans le sens de la marche
        float aheadT = Mathf.Clamp01(segmentT + 0.05f);
        Vector3 aheadPosition = CatmullRom(aheadT, p0, p1, p2, p3);
        Vector3 direction = (aheadPosition - position).normalized;

        if (direction.sqrMagnitude > 0.0001f)
            rb.MoveRotation(Quaternion.LookRotation(direction));

        rb.MovePosition(position);

        CheckDerailRisk(position, direction, segmentLength, p0, p1, p2, p3);
    }

    private void CheckDerailRisk(Vector3 position, Vector3 direction, float segmentLength,
        Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        float lookaheadT = curveLookahead / Mathf.Max(segmentLength, 0.01f);
        float farT = Mathf.Clamp01(segmentT + lookaheadT);

        Vector3 farPosition = CatmullRom(farT, p0, p1, p2, p3);
        Vector3 farDirection = (farPosition - position).normalized;

        float turnAngle = Vector3.Angle(direction, farDirection);
        float curveSeverity = Mathf.Clamp01(turnAngle / maxSafeTurnAngle);
        float safeSpeedLimit = Mathf.Lerp(maxSpeed, minDerailSpeed, curveSeverity);

        if (currentSpeed > safeSpeedLimit)
        {
            Derail();
        }
    }

    private void Derail()
    {
        if (isDerailed) return;

        isDerailed = true;
        currentSpeed = 0f;
        Debug.Log("Le train a déraillé : vitesse trop élevée pour ce virage !");

        if (bodyVisual != null)
            bodyVisual.localRotation = Quaternion.Euler(0f, 0f, 25f);
    }

    // Formule standard pour faire passer une courbe douce par une série de points.
    // t va de 0 à 1 entre p1 et p2 ; p0 et p3 servent juste à donner la bonne "tangente".
    private Vector3 CatmullRom(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        float t2 = t * t;
        float t3 = t2 * t;

        Vector3 a = 2f * p1;
        Vector3 b = p2 - p0;
        Vector3 c = 2f * p0 - 5f * p1 + 4f * p2 - p3;
        Vector3 d = -p0 + 3f * p1 - 3f * p2 + p3;

        return 0.5f * (a + b * t + c * t2 + d * t3);
    }

    private void UpdateCinematicCamera()
    {
        if (vehicleCamera == null || cameraAnchors == null || cameraAnchors.Length == 0)
            return;

        cameraTimer += Time.deltaTime;
        if (cameraTimer >= cameraHoldDuration)
        {
            cameraTimer = 0f;
            currentCameraAnchor = (currentCameraAnchor + 1) % cameraAnchors.Length;

            // On mémorise d'où part la transition, pour une interpolation propre sur toute sa durée
            transitionStartPosition = vehicleCamera.transform.position;
            transitionProgress = 0f;
        }

        Transform anchor = cameraAnchors[currentCameraAnchor];
        if (anchor == null) return;

        transitionProgress = Mathf.Clamp01(transitionProgress + Time.deltaTime / cameraTransitionDuration);

        // Smoothstep : accélère doucement au départ, ralentit doucement à l'arrivée.
        // Beaucoup plus naturel qu'une vitesse constante qui "chasse" sa cible.
        float easedT = transitionProgress * transitionProgress * (3f - 2f * transitionProgress);

        vehicleCamera.transform.position = Vector3.Lerp(transitionStartPosition, anchor.position, easedT);

        // On regarde le train en continu, à n'importe quel instant de la transition
        Vector3 lookTarget = transform.position + Vector3.up * 1.5f;
        Quaternion targetRotation = Quaternion.LookRotation(lookTarget - vehicleCamera.transform.position);
        vehicleCamera.transform.rotation = Quaternion.Slerp(
            vehicleCamera.transform.rotation, targetRotation, 10f * Time.deltaTime);
    }

    public override void EnterVehicle(GameObject driver)
    {
        base.EnterVehicle(driver);

        currentSpeed = 0f;
        isDerailed = false;
        if (bodyVisual != null)
            bodyVisual.localRotation = Quaternion.identity;

        currentCameraAnchor = 0;
        cameraTimer = 0f;
        transitionProgress = 1f; // la caméra se place net sur la première ancre, sans glisser depuis l'ancienne vue
    }

    public override void ExitVehicle(GameObject driver)
    {
        base.ExitVehicle(driver);
        currentSpeed = 0f;
    }

    // --- Visualisation du chemin ---

    private void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Length < 4)
            return;

        int n = waypoints.Length;

        Gizmos.color = Color.yellow;
        foreach (Transform wp in waypoints)
        {
            if (wp != null)
                Gizmos.DrawSphere(wp.position, 0.5f);
        }

        Gizmos.color = Color.cyan;
        for (int i = 0; i < n; i++)
        {
            Transform t0 = waypoints[(i - 1 + n) % n];
            Transform t1 = waypoints[i % n];
            Transform t2 = waypoints[(i + 1) % n];
            Transform t3 = waypoints[(i + 2) % n];

            if (t0 == null || t1 == null || t2 == null || t3 == null)
                continue;

            Vector3 p0 = t0.position, p1 = t1.position, p2 = t2.position, p3 = t3.position;
            Vector3 previousPoint = p1;
            const int steps = 20;

            for (int s = 1; s <= steps; s++)
            {
                float t = s / (float)steps;
                Vector3 point = CatmullRom(t, p0, p1, p2, p3);
                Gizmos.DrawLine(previousPoint, point);
                previousPoint = point;
            }
        }
    }

    // --- Génération d'un vrai visuel de rails (clic droit sur le composant dans l'Inspector) ---

    [ContextMenu("Générer le visuel des rails")]
    private void GenerateRailVisual()
    {
        if (waypoints == null || waypoints.Length < 4)
        {
            Debug.LogWarning("Il faut au moins 4 points de passage pour générer les rails.");
            return;
        }

        // On supprime l'ancien visuel si on régénère après avoir bougé des points
        Transform existing = GameObject.Find("RailsVisual")?.transform;
        if (existing != null)
            DestroyImmediate(existing.gameObject);

        GameObject railsRoot = new GameObject("RailsVisual");

        int n = waypoints.Length;
        const int stepsPerSegment = 10;

        for (int i = 0; i < n; i++)
        {
            Transform t0 = waypoints[(i - 1 + n) % n];
            Transform t1 = waypoints[i % n];
            Transform t2 = waypoints[(i + 1) % n];
            Transform t3 = waypoints[(i + 2) % n];

            Vector3 p0 = t0.position, p1 = t1.position, p2 = t2.position, p3 = t3.position;
            Vector3 previousPoint = p1;

            for (int s = 1; s <= stepsPerSegment; s++)
            {
                float t = s / (float)stepsPerSegment;
                Vector3 point = CatmullRom(t, p0, p1, p2, p3);
                CreateRailSegment(railsRoot.transform, previousPoint, point);
                previousPoint = point;
            }
        }
    }

    private void CreateRailSegment(Transform parent, Vector3 from, Vector3 to)
    {
        GameObject segment = GameObject.CreatePrimitive(PrimitiveType.Cube);
        segment.name = "RailSegment";
        segment.transform.SetParent(parent);

        float length = Vector3.Distance(from, to);
        segment.transform.position = (from + to) / 2f;
        segment.transform.rotation = Quaternion.LookRotation(to - from);
        segment.transform.localScale = new Vector3(2f, 0.2f, length);

        // Purement visuel : on retire le Collider pour ne gêner ni la détection du train ni les déplacements
        Collider col = segment.GetComponent<Collider>();
        if (col != null)
            DestroyImmediate(col);
    }
}