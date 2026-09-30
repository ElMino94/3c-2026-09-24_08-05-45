using UnityEngine;

// À mettre sur le modèle du personnage (celui qui a le composant Animator), enfant du Player.
[RequireComponent(typeof(Animator))]
public class PlayerAnimation : MonoBehaviour
{
    [Tooltip("Laisse vide : il est trouvé automatiquement sur le Player")]
    [SerializeField] private CharacterController controller;

    private Animator animator;
    private static readonly int SpeedHash = Animator.StringToHash("Speed");

    private void Awake()
    {
        animator = GetComponent<Animator>();
        if (controller == null)
            controller = GetComponentInParent<CharacterController>();
    }

    private void Update()
    {
        // Vitesse horizontale uniquement (on ignore la chute/le saut)
        Vector3 flatVelocity = controller.velocity;
        flatVelocity.y = 0f;
        float speed = flatVelocity.magnitude;

        animator.SetFloat(SpeedHash, speed);
    }
}