using UnityEngine;

public class RifleShooter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;

    [Header("Tir")]
    [SerializeField] private float damage = 35f;
    [SerializeField] private float range = 100f;
    [SerializeField] private float fireRate = 0.3f;

    private Animator weaponAnimator;
    private PlayerControls controls;
    private float nextFireTime;
    private static readonly int FireHash = Animator.StringToHash("Fire");

    private void Awake() {

        weaponAnimator = GetComponent<Animator>();
        controls = new PlayerControls();

        if (playerCamera == null) playerCamera = Camera.main;

    }

    private void OnEnable() {

        controls.Player.Enable();
        
    }

    private void OnDisable() {

        controls.Player.Disable();

    }

    private void Update() {

        if(controls.Player.Fire.WasPressedThisFrame() && Time.time >= nextFireTime) {

            Shoot();

        }
        
    }

    private void Shoot() {

        nextFireTime = Time.time + fireRate;

        weaponAnimator.SetTrigger(FireHash);

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        if(Physics.Raycast(ray, out RaycastHit hit, range)) {

            Debug.DrawLine(ray.origin, hit.point, Color.red, 1f);

            Health targetHealth = hit.collider.GetComponent<Health>();

            if(targetHealth != null) {

                targetHealth.TakeDamage(damage);

            }

        } else {

            Debug.DrawRay(ray.origin, ray.direction * range, Color.yellow, 1f);

        }



    }



}
