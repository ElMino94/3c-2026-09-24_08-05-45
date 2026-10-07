using UnityEngine;

public abstract class VehicleBase : MonoBehaviour
{
    [Header("Véhicule - Général")]
    [Tooltip("Caméra à activer quand on est dans le véhicule")]
    [SerializeField] protected Camera vehicleCamera;
    [Tooltip("Endroit où le joueur réapparaît en sortant du véhicule")]
    [SerializeField] protected Transform exitPoint;

    public bool IsOccupied {
        
        get;
        private set;
    
    }

    public virtual void EnterVehicle(GameObject driver) {

        IsOccupied = true;

        if (vehicleCamera != null)
            vehicleCamera.gameObject.SetActive(true);

    }

    public virtual void ExitVehicle(GameObject driver) {

        IsOccupied = false;

        if (vehicleCamera != null)
            vehicleCamera.gameObject.SetActive(false);

    }

    public Vector3 GetExitPosition() {

        return exitPoint != null ? exitPoint.position : transform.position;

    }
}