using UnityEngine;
using UnityEngine.Events;
using System;

namespace Five.InputHandlers
{
    public class MobileInputHandler : MonoBehaviour
    {
        public enum SwipeDirection { Up, Down, Left, Right }

        [Header("Swipe Settings")]
        [SerializeField] private float minSwipeDistance = 5f;
        [SerializeField] private float cooldownBetweenSwipes = 0.5f;

        // State variables
        private bool touchBegan;
        private int fingerId;
        private Vector3 initialSwipePosition;
        private Vector3 currentSwipePosition;
        private float touchStartTime;
        private float lastSwipeTime;
        private bool isSwipeReady = true;
        private Camera cameraInput;
        private bool isInCooldown;
        private bool swipeDetectedThisTouch;

        // Events
        public UnityEvent TouchBegan { get; } = new();
        public UnityEvent TouchEnded { get; } = new();
        public UnityEvent<Collider> TouchHitCollider { get; } = new();
        public UnityEvent<SwipeDirection> SwipeDetected { get; } = new();

        public Func<bool> IsPointerOverUI;
        public bool Interactable { get; set; } = true;
        public bool IsTouching => touchBegan;
        public Vector2 SwipeDelta => touchBegan ? currentSwipePosition - initialSwipePosition : Vector2.zero;
        public bool IsSwipeReady => isSwipeReady;

        private Camera CameraInput => cameraInput != null ? cameraInput : (cameraInput = Camera.main);

        private void Update()
        {
            if (!Interactable) return;

            if (isInCooldown && Time.time - lastSwipeTime >= cooldownBetweenSwipes)
            {
                isInCooldown = false;
                isSwipeReady = true;
                if (touchBegan)
                {
                    initialSwipePosition = currentSwipePosition;
                }
            }

            HandleTouchInput();
            HandleMouseInput();
        }

        private void HandleTouchInput()
        {
            if (Input.touchCount == 0) return;

            bool touchStart = false;
            bool touchEnded = false;

            Touch? touch = null;

            if (!touchBegan)
            {
                for (int i = 0; i < Input.touchCount; i++)
                {
                    if (Input.touches[i].phase == TouchPhase.Began)
                    {
                        touch = Input.touches[i];
                        StartNewTouch(touch.Value);
                        touchStart = true;
                        break;
                    }
                }
            }
            else
            {
                for (int i = 0; i < Input.touchCount; i++)
                {
                    if (Input.touches[i].fingerId == fingerId)
                    {
                        touch = Input.touches[i];
                        UpdateTouchPosition(touch.Value.position);

                        if (touch.Value.phase == TouchPhase.Moved && isSwipeReady && !swipeDetectedThisTouch)
                        {
                            CheckForContinuousSwipe();
                        }
                        break;
                    }
                }

                if (touch == null)
                {
                    touchBegan = false;
                }
                else
                {
                    touchEnded = touch.Value.phase == TouchPhase.Ended ||
                                touch.Value.phase == TouchPhase.Canceled;
                }
            }

            ProcessTouchEvents(touchStart, touchEnded);
        }

        private void HandleMouseInput()
        {
            if (Input.touchCount > 0) return;

            if (!touchBegan && Input.GetMouseButtonDown(0))
            {
                StartNewTouch(Input.mousePosition);
                ProcessTouchEvents(true, false);
            }

            if (touchBegan)
            {
                UpdateTouchPosition(Input.mousePosition);

                if (Input.GetMouseButton(0) && isSwipeReady && !swipeDetectedThisTouch)
                {
                    CheckForContinuousSwipe();
                }

                if (Input.GetMouseButtonUp(0))
                {
                    ProcessTouchEvents(false, true);
                }
            }
        }

        private void StartNewTouch(Touch touch)
        {
            touchBegan = true;
            fingerId = touch.fingerId;
            initialSwipePosition = touch.position;
            currentSwipePosition = initialSwipePosition;
            touchStartTime = Time.time;
            swipeDetectedThisTouch = false;
            TouchBegan.Invoke();
        }

        private void StartNewTouch(Vector3 position)
        {
            touchBegan = true;
            fingerId = -1;
            initialSwipePosition = position;
            currentSwipePosition = initialSwipePosition;
            touchStartTime = Time.time;
            swipeDetectedThisTouch = false;
            TouchBegan.Invoke();
        }

        private void UpdateTouchPosition(Vector3 newPosition)
        {
            currentSwipePosition = newPosition;
        }

        private void ProcessTouchEvents(bool touchStart, bool touchEnded)
        {
            if (touchStart)
            {
                if (IsPointerOverUI == null || !IsPointerOverUI())
                {
                    CheckForColliderHit();
                }
            }

            if (touchEnded)
            {
                swipeDetectedThisTouch = false;
                touchBegan = false;
                TouchEnded.Invoke();
            }
        }

        private void CheckForColliderHit()
        {
            Ray ray = CameraInput.ScreenPointToRay(currentSwipePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                TouchHitCollider.Invoke(hit.collider);
            }
        }

        private void CheckForContinuousSwipe()
        {
            if (isInCooldown || swipeDetectedThisTouch) return;

            Vector2 swipeDelta = currentSwipePosition - initialSwipePosition;

            if (swipeDelta.magnitude >= minSwipeDistance)
            {
                ProcessSwipe(swipeDelta);
                StartCooldown();
                swipeDetectedThisTouch = true;
            }
        }

        private void ProcessSwipe(Vector2 swipeDelta)
        {
            swipeDelta.Normalize();

            if (Mathf.Abs(swipeDelta.y) > Mathf.Abs(swipeDelta.x))
            {
                SwipeDetected.Invoke(swipeDelta.y > 0 ? SwipeDirection.Up : SwipeDirection.Down);
            }
            else
            {
                SwipeDetected.Invoke(swipeDelta.x > 0 ? SwipeDirection.Right : SwipeDirection.Left);
            }
        }

        private void StartCooldown()
        {
            isSwipeReady = false;
            isInCooldown = true;
            lastSwipeTime = Time.time;
        }

        public void SetCameraInput(Camera targetCamera)
        {
            cameraInput = targetCamera;
        }
    }
}