using UnityEngine;

namespace StylizedCars.Demo
{
    public class UIToggleAnimationButton : MonoBehaviour
    {
        public LocalPositionAnimator targetAnimator;

        public void ToggleAnimation()
        {
            if (targetAnimator != null)
            {
                targetAnimator.Toggle();
            }
        }
    }
}