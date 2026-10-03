using UnityEngine;
using UnityEngine.UIElements;

// Contrôleur de voiture. Hérite de VehicleBase pour profiter du système d'entrée/sortie déjà en place.
public class CarController : VehicleBase
{
    [Header("Accélération")]
    [Tooltip("Accélération lente, comme demandé dans le cahier des charges")]
    [SerializeField] private float accelerationRate = 4f;
    [SerializeField] private float reverseAccelerationRate = 3f;
    [SerializeField] private float maxForwardSpeed = 20f;
    [SerializeField] private float maxReverseSpeed = 8f;
    [Tooltip("Freinage moyen")]
    [SerializeField] private float brakeDeceleration = 10f;
    [Tooltip("Forte inertie : la voiture ralentit très doucement si on ne touche à rien")]
    [SerializeField] private float coastingDeceleration = 1.5f;

    [Header("Direction")]
    [SerializeField] private float maxSteeringAngle = 35f;
    [Tooltip("Vitesse à laquelle le volant tourne quand on tient Q ou D (degrés/seconde)")]
    [SerializeField] private float steeringAccumulationSpeed = 60f;
    [Tooltip("Vitesse à laquelle le volant se recentre PENDANT qu'on accélère (degrés/seconde)")]
    [SerializeField] private float steeringReturnSpeed = 90f;
    [Tooltip("Vitesse de rotation de la voiture (degrés/seconde) volant braqué à fond, à vitesse max")]
    [SerializeField] private float maxTurnRate = 45f;

    [Header("Caméra de suivi")]
    [SerializeField] private Vector3 cameraOffset = new Vector3(0f, 3f, -6f);
    [SerializeField] private float cameraFollowSpeed = 6f;
    [SerializeField] private float minFOV = 60f;
    [SerializeField] private float maxFOV = 75f;

    private PlayerControls controls;
    private float currentSpeed;  // positif = avant, négatif = marche arrière
    private float steeringAngle; // en degrés, négatif = à gauche, positif = à droite

    private void Awake()
    {
        controls = new PlayerControls();
    }

    private void OnEnable() => controls.Player.Enable();
    private void OnDisable() => controls.Player.Disable();

    private void Update()
    {
        if (!IsOccupied)
            return;

        HandleSteering();
        HandleSpeed();
        ApplyMovement();
    }

    private void LateUpdate()
    {
        if (!IsOccupied)
            return;

        FollowCamera();
        UpdateFOV();
    }

    private void HandleSteering()
    {
        Vector2 move = controls.Player.Move.ReadValue<Vector2>();
        bool accelerating = move.y > 0.1f;
        bool steeringInput = Mathf.Abs(move.x) > 0.01f;

        // Le volant est cumulatif : il continue de tourner tant qu'on tient Q ou D
        steeringAngle += move.x * steeringAccumulationSpeed * Time.deltaTime;
        steeringAngle = Mathf.Clamp(steeringAngle, -maxSteeringAngle, maxSteeringAngle);

        // Il se remet droit progressivement en accélérant, mais seulement si on ne
        // tient pas Q/D en même temps (sinon le recentrage annulerait ton virage)
        if (accelerating && !steeringInput)
        {
            steeringAngle = Mathf.MoveTowards(steeringAngle, 0f, steeringReturnSpeed * Time.deltaTime);
        }
    }

    private void HandleSpeed()
    {
        Vector2 move = controls.Player.Move.ReadValue<Vector2>();
        bool accelerating = move.y > 0.1f;
        bool braking = move.y < -0.1f;
        bool reversing = controls.Player.Reverse.IsPressed();

        if (accelerating)
        {
            currentSpeed += accelerationRate * Time.deltaTime;
        }
        else if (braking)
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, brakeDeceleration * Time.deltaTime);
        }
        else if (reversing)
        {
            currentSpeed -= reverseAccelerationRate * Time.deltaTime;
        }
        else
        {
            // Forte inertie : on ralentit très doucement quand on ne touche à rien
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, coastingDeceleration * Time.deltaTime);
        }

        currentSpeed = Mathf.Clamp(currentSpeed, -maxReverseSpeed, maxForwardSpeed);
    }

    private void ApplyMovement()
    {
        // L'amplitude du virage dépend de la vitesse : pas de rotation à l'arrêt
        float speedRatio = currentSpeed / maxForwardSpeed;
        float steerRatio = steeringAngle / maxSteeringAngle;
        float turnAmount = steerRatio * speedRatio * maxTurnRate * Time.deltaTime;

        transform.Rotate(Vector3.up, turnAmount);
        transform.position += transform.forward * currentSpeed * Time.deltaTime;
    }

    private void FollowCamera()
    {
        if (vehicleCamera == null) return;

        Vector3 targetPosition = transform.TransformPoint(cameraOffset);
        vehicleCamera.transform.position = Vector3.Lerp(
            vehicleCamera.transform.position, targetPosition, cameraFollowSpeed * Time.deltaTime);

        Vector3 lookTarget = transform.position + Vector3.up * 1.5f;
        Quaternion targetRotation = Quaternion.LookRotation(lookTarget - vehicleCamera.transform.position);
        vehicleCamera.transform.rotation = Quaternion.Slerp(
            vehicleCamera.transform.rotation, targetRotation, cameraFollowSpeed * Time.deltaTime);
    }

    private void UpdateFOV()
    {
        if (vehicleCamera == null) return;

        float speedRatio = Mathf.Abs(currentSpeed) / maxForwardSpeed;
        vehicleCamera.fieldOfView = Mathf.Lerp(minFOV, maxFOV, speedRatio);
    }

    public override void EnterVehicle(GameObject driver)
    {
        base.EnterVehicle(driver);
        currentSpeed = 0f;
        steeringAngle = 0f;
    }

    public override void ExitVehicle(GameObject driver)
    {
        base.ExitVehicle(driver);
        currentSpeed = 0f;
    }
}