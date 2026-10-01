using UnityEngine;

namespace StylizedCars.Demo
{
    public class MouseDragOrbitCamera : MonoBehaviour
    {
        [Header("Target")]
        public Transform target;

        [Header("Distance")]
        public float distance = 5f;
        public float height = 1.5f;

        [Header("Rotation")]
        public float dragSpeed = 4f;
        public float damping = 8f;

        [Header("Limits")]
        public bool invertDrag = false;

        private float currentYaw;
        private float targetYaw;
        private float yawVelocity;

        void Start()
        {
            if (target == null)
            {
                Debug.LogWarning("No target assigned to MouseDragOrbitCamera.");
                enabled = false;
                return;
            }

            Vector3 angles = transform.eulerAngles;
            currentYaw = angles.y;
            targetYaw = currentYaw;
        }

        void Update()
        {
            HandleInput();
        }

        void LateUpdate()
        {
            SmoothRotation();
            UpdatePosition();
        }

        void HandleInput()
        {
            if (Input.GetMouseButton(0))
            {
                float mouseX = Input.GetAxis("Mouse X");

                if (invertDrag)
                    mouseX = -mouseX;

                targetYaw += mouseX * dragSpeed;
            }
        }

        void SmoothRotation()
        {
            currentYaw = Mathf.SmoothDampAngle(
                currentYaw,
                targetYaw,
                ref yawVelocity,
                1f / damping
            );
        }

        void UpdatePosition()
        {
            Quaternion rotation = Quaternion.Euler(0f, currentYaw, 0f);

            Vector3 offset = rotation * new Vector3(0f, 0f, -distance);

            transform.position = target.position + Vector3.up * height + offset;

            transform.LookAt(target.position + Vector3.up * height * 0.5f);
        }
    }
}