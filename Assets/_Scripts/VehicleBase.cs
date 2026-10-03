using UnityEngine;

// Classe de base pour tout véhicule utilisable (vélo/voiture, hélicoptère, train...).
// Chaque véhicule concret doit créer un script qui hérite de celle-ci.
public abstract class VehicleBase : MonoBehaviour
{
    [Header("Véhicule - Général")]
    [Tooltip("Caméra à activer quand on est dans le véhicule")]
    [SerializeField] protected Camera vehicleCamera;
    [Tooltip("Endroit où le joueur réapparaît en sortant du véhicule")]
    [SerializeField] protected Transform exitPoint;

    public bool IsOccupied { get; private set; }

    // Appelée par VehicleInteraction quand le joueur monte dans le véhicule.
    // Les classes filles peuvent surcharger (override) cette méthode pour ajouter
    // leur propre logique : activer leur script de pilotage, démarrer un moteur...
    public virtual void EnterVehicle(GameObject driver)
    {
        IsOccupied = true;

        if (vehicleCamera != null)
            vehicleCamera.gameObject.SetActive(true);
    }

    public virtual void ExitVehicle(GameObject driver)
    {
        IsOccupied = false;

        if (vehicleCamera != null)
            vehicleCamera.gameObject.SetActive(false);
    }

    public Vector3 GetExitPosition()
    {
        return exitPoint != null ? exitPoint.position : transform.position;
    }
}