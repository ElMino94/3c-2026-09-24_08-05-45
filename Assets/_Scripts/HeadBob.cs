using UnityEngine;

public class HeadBob : MonoBehaviour {

    [Header("Références")]
    [Tooltip("Laisse vide : il est trouvé automatiquement sur le Player")]
    [SerializeField] private CharacterController controller;

    [Header("Forme du balancement")]
    [Tooltip("Un cycle complet de 0 à 1. Par défaut c'est une sinusoïde, modifie-la pour changer le rythme du pas.")]
    [SerializeField]
    private AnimationCurve bobCurve = new AnimationCurve(new Keyframe(0f, 0f, 6.283f, 6.283f), new Keyframe(0.25f, 1f, 0f, 0f), new Keyframe(0.5f, 0f, -6.283f, -6.283f), new Keyframe(0.75f, -1f, 0f, 0f), new Keyframe(1f, 0f, 6.283f, 6.283f));

    [Header("Intensité")]
    [SerializeField] private float verticalAmount = 0.05f;   
    [SerializeField] private float horizontalAmount = 0.03f; 

    [Header("Rythme")]
    [Tooltip("Nombre de cycles (2 pas) par mètre parcouru. Plus c'est grand, plus les pas sont rapprochés.")]
    [SerializeField] private float cyclesPerMeter = 0.25f;
    [Tooltip("Vitesse à laquelle l'intensité vaut 1. Mets ta vitesse de marche.")]
    [SerializeField] private float referenceSpeed = 5f;
    [Tooltip("Vitesse à laquelle le balancement apparaît ou disparaît")]
    [SerializeField] private float blendSpeed = 8f;

    private Vector3 startLocalPos;
    private float cycle;      
    private float amplitude;  

    private void Awake() {

        if (controller == null)
            controller = GetComponentInParent<CharacterController>();

    }

    private void Start() {

        startLocalPos = transform.localPosition;

    }

    private void LateUpdate() {

        Vector3 flatVelocity = controller.velocity;
        flatVelocity.y = 0f;
        float speed = flatVelocity.magnitude;

        bool grounded = controller.isGrounded;

        float target = grounded ? Mathf.Clamp(speed / referenceSpeed, 0f, 1.5f) : 0f;
        amplitude = Mathf.Lerp(amplitude, target, blendSpeed * Time.deltaTime);

        if (grounded)
            cycle += speed * cyclesPerMeter * Time.deltaTime;
        cycle = Mathf.Repeat(cycle, 1f);

        float vertical = bobCurve.Evaluate(Mathf.Repeat(cycle * 2f, 1f)) * verticalAmount;
        float horizontal = bobCurve.Evaluate(cycle) * horizontalAmount;

        transform.localPosition = startLocalPos + new Vector3(horizontal, vertical, 0f) * amplitude;

    }

}