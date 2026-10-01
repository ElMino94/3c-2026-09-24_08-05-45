using UnityEngine;
using UnityEngine.Events;
using System.Collections;

namespace StylizedCars.Demo
{
    public class LocalPositionAnimator : MonoBehaviour
    {
        [Header("Animation")]
        public Vector3 startPosition;
        public Vector3 endPosition;

        public float animationSpeed = 2f;
        public AnimationCurve animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Settings")]
        public bool playOnStart = false;

        [Header("Events")]
        public UnityEvent onAnimateIn;
        public UnityEvent onAnimateOut;

        private Coroutine animationRoutine;
        private bool isOpened;

        void Start()
        {
            transform.localPosition = startPosition;

            if (playOnStart)
            {
                AnimateIn();
            }
        }

        public void Toggle()
        {
            if (isOpened)
                AnimateOut();
            else
                AnimateIn();
        }

        public void AnimateIn()
        {
            StartAnimation(startPosition, endPosition);
            isOpened = true;
            onAnimateIn?.Invoke();
        }

        public void AnimateOut()
        {
            StartAnimation(endPosition, startPosition);
            isOpened = false;
            onAnimateOut?.Invoke();
        }

        void StartAnimation(Vector3 from, Vector3 to)
        {
            if (animationRoutine != null)
                StopCoroutine(animationRoutine);

            animationRoutine = StartCoroutine(AnimateRoutine(from, to));
        }

        IEnumerator AnimateRoutine(Vector3 from, Vector3 to)
        {
            float time = 0f;

            while (time < 1f)
            {
                time += Time.deltaTime * animationSpeed;

                float curveValue = animationCurve.Evaluate(time);

                transform.localPosition = Vector3.LerpUnclamped(from, to, curveValue);

                yield return null;
            }

            transform.localPosition = to;
        }
    }
}