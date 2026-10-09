using UnityEngine;
using TMPro;

// À mettre sur le Player.
public class VehicleInteraction : MonoBehaviour
{
    [Header("Détection")]
    [SerializeField] private float interactionRange = 3f;

    [Header("Références joueur")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private CharacterController playerMovementController;
    [SerializeField] private Camera playerCamera;

    [Header("UI (optionnel)")]
    [Tooltip("Texte affiché quand un véhicule est à portée. Laisse vide si tu n'en as pas encore.")]
    [SerializeField] private TMP_Text promptText;

    private PlayerControls controls;
    private VehicleBase currentVehicle;
    private VehicleBase nearbyVehicle;
    private Renderer[] playerRenderers;

    private void Awake()
    {
        controls = new PlayerControls();
        // Récupéré une seule fois : tous les Renderer du joueur (capsule, et un éventuel
        // modèle ou arme si tu en ajoutes un plus tard), pour pouvoir les cacher/remontrer.
        playerRenderers = GetComponentsInChildren<Renderer>();
    }

    private void OnEnable() => controls.Player.Enable();
    private void OnDisable() => controls.Player.Disable();

    private void Update()
    {
        if (currentVehicle == null)
            DetectNearbyVehicle();

        if (controls.Player.Interact.WasPressedThisFrame())
        {
            if (currentVehicle != null)
                ExitVehicle();
            else if (nearbyVehicle != null)
                EnterVehicle(nearbyVehicle);
        }
    }

    private void DetectNearbyVehicle()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, interactionRange);
        nearbyVehicle = null;
        float closestDist = float.MaxValue;

        foreach (Collider hit in hits)
        {
            VehicleBase vehicle = hit.GetComponentInParent<VehicleBase>();
            if (vehicle != null && !vehicle.IsOccupied)
            {
                float dist = Vector3.Distance(transform.position, vehicle.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    nearbyVehicle = vehicle;
                }
            }
        }

        if (promptText != null)
            promptText.gameObject.SetActive(nearbyVehicle != null);
    }

    private void EnterVehicle(VehicleBase vehicle)
    {
        currentVehicle = vehicle;
        vehicle.EnterVehicle(gameObject);

        playerController.enabled = false;
        playerMovementController.enabled = false;
        playerCamera.gameObject.SetActive(false);
        SetPlayerVisible(false);

        if (promptText != null)
            promptText.gameObject.SetActive(false);
    }

    private void ExitVehicle()
    {
        currentVehicle.ExitVehicle(gameObject);

        // On désactive le Character Controller le temps de déplacer manuellement
        // le joueur : il refuse sinon qu'on touche à sa position directement.
        playerMovementController.enabled = false;
        transform.position = currentVehicle.GetExitPosition();
        playerMovementController.enabled = true;

        playerController.enabled = true;
        playerCamera.gameObject.SetActive(true);
        SetPlayerVisible(true);

        currentVehicle = null;
    }

    private void SetPlayerVisible(bool visible)
    {
        foreach (Renderer r in playerRenderers)
        {
            if (r != null)
                r.enabled = visible;
        }
    }
}