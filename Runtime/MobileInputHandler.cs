using UnityEngine;
using UnityEngine.Events;

public class MobileInputHandler : MonoBehaviour
{
    private bool touchBegan;

    private int fingerId;
    private Vector3 initialScreenPosition;
    private Camera cameraInput;

    public bool Interactable { get; set; }
    public bool IsTouching => touchBegan;
    public UnityEvent TouchBegan { get; } = new();
    public UnityEvent TouchEnded { get; } = new();
    public UnityEvent<Collider2D> TouchHitCollider { get; } = new();

    private Camera CameraInput
    {
        get
        {
            if (cameraInput == null)
            {
                cameraInput = Camera.main;
            }

            return cameraInput;
        }
    }

    private void Update()
    {
        if (!Interactable) return;

        var touchStart = false;
        var touchEnded = false;
        var inputPosition = new Vector3();

        if (Input.touchCount > 0)
        {
            Touch? touch = null;
            if (!touchBegan)
            {
                for (int i = 0; i < Input.touchCount; i++)
                {
                    if (Input.touches[i].phase == TouchPhase.Began)
                    {
                        touchBegan = true;
                        touch = Input.touches[i];
                        fingerId = touch.Value.fingerId;
                        TouchBegan.Invoke();
                        break;
                    }
                }

                if (touch.HasValue && touchBegan)
                    initialScreenPosition = touch.Value.position;
            }
            else
            {
                for (int i = 0; i < Input.touchCount; i++)
                {
                    if (Input.touches[i].fingerId == fingerId)
                    {
                        touch = Input.touches[i];
                        break;
                    }
                }
            }

            if (touch == null)
            {
                touchBegan = false;
            }
            else
            {
                touchStart = touch.Value.phase == TouchPhase.Began;
                touchEnded = touch.Value.phase == TouchPhase.Ended;
                inputPosition = CameraInput.ScreenToWorldPoint(touch.Value.position);

                if (touchEnded)
                    touchBegan = false;
            }
        }
        else
        {
            //make the mouse logic is the same as the touch system logic above
            if (!touchBegan)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    touchBegan = true;
                    TouchBegan.Invoke();
                }

                if (touchBegan)
                    initialScreenPosition = Input.mousePosition;
            }

            if (touchBegan)
            {
                touchStart = Input.GetMouseButtonDown(0);
                touchEnded = Input.GetMouseButtonUp(0);
                inputPosition = CameraInput.ScreenToWorldPoint(Input.mousePosition);

                if (touchEnded)
                    touchBegan = false;
            }
        }

        if (touchStart)
        {
            var hit = Physics2D.Raycast(inputPosition, Vector2.zero);
            if (hit.collider != null)
            {
                TouchHitCollider.Invoke(hit.collider);
            }
        }


        if (touchEnded)
        {
            touchBegan = false;
            TouchEnded.Invoke();
        }
    }

    public void SetCameraInput(Camera targetCamera)
    {
        cameraInput = targetCamera;
    }
}