using UnityEngine;

// À mettre sur la Main Camera (l'enfant de CameraHolder), pas sur le Player.
[RequireComponent(typeof(Camera))]
public class SprintFOV : MonoBehaviour
{
    [Header("FOV")]
    [SerializeField] private float normalFOV = 60f;
    [SerializeField] private float sprintFOV = 70f;

    [Header("Transition")]
    [SerializeField] private float transitionDuration = 0.25f;
    [Tooltip("X = avancement de la transition (0 à 1), Y = interpolation (0 = FOV normal, 1 = FOV sprint)")]
    [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private Camera cam;
    private PlayerControls controls;
    private float progress; // 0 = FOV normal, 1 = FOV sprint

    private void Awake()
    {
        cam = GetComponent<Camera>();
        controls = new PlayerControls();
    }

    private void OnEnable()
    {
        controls.Player.Enable();
    }

    private void OnDisable()
    {
        controls.Player.Disable();
    }

    private void Start()
    {
        cam.fieldOfView = normalFOV;
    }

    private void Update()
    {
        bool sprinting = controls.Player.Sprint.IsPressed() && controls.Player.Move.ReadValue<Vector2>().sqrMagnitude > 0.01f;

        progress += (sprinting ? 1f : -1f) * Time.deltaTime / transitionDuration;
        progress = Mathf.Clamp01(progress);

        float t = transitionCurve.Evaluate(progress);
        cam.fieldOfView = Mathf.Lerp(normalFOV, sprintFOV, t);
    }
}