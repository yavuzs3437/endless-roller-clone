using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerBehaviour : MonoBehaviour
{ 
    private Rigidbody rb;

    public float dodgeSpeed = 5;

    public float rollSpeed = 5;

    public enum MobileHorizMovement
    {
        Accelerometer,
        ScreenTouch
    }

    public MobileHorizMovement horizMovement = MobileHorizMovement.Accelerometer;

    public float swipeMove = 2f;

    public float minSwipeDistance = 0.25f;

    private float minSwipeDistancePixels;

    private Vector2 touchStart;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();

        minSwipeDistancePixels = minSwipeDistance * Screen.dpi;
    }

    private void FixedUpdate()
    {
        var horizontalSpeed = Input.GetAxis("Horizontal") * dodgeSpeed;

#if UNITY_STANDALONE || UNITY_WEBPLAYER || UNITY_EDITOR
        horizontalSpeed = Input.GetAxis("Horizontal") * dodgeSpeed;
        if (Input.GetMouseButton(0))
        {
            horizontalSpeed = CalculateMovement(Input.mousePosition);
        }

#elif UNITY_IOS || UNITY_ANDROID

            if(horizMovement == MobileHorizMovement.Accelerometer)
            {
                // Move player based on direction of the accelerometer
                horizontalSpeed = Input.acceleration.x * dodgeSpeed;
            }

            //Check if Input has registered more than zero touches 
            if (Input.touchCount > 0)
            {
                if (horizMovement == MobileHorizMovement.ScreenTouch)
                {
                    //Store the first touch detected. 
                    Touch touch = Input.touches[0];
                    horizontalSpeed = CalculateMovement(touch.position);
                }
            }
#endif

        rb.AddForce(horizontalSpeed, 0, rollSpeed);
    }

    private void Update()
    {
#if UNITY_IOS || UNITY_ANDROID
        if (Input.touchCount > 0)
        { 
            Touch touch = Input.touches[0];

            SwipeTeleport(touch);

            TouchObjects(touch);

            ScalePlayer();
        }
#endif
    }
    private void SwipeTeleport(Touch touch)
    {
        if (touch.phase == TouchPhase.Began)
        {
            touchStart = touch.position;
        }

        else if (touch.phase == TouchPhase.Ended)
        { 
            Vector2 touchEnd = touch.position;
            float x = touchEnd.x - touchStart.x;
            if (Mathf.Abs(x) < minSwipeDistancePixels)
            {
                return;
            }

            Vector3 moveDirection;

            if (x < 0)
            {
                moveDirection = Vector3.left;
            }
            else
            { 
                moveDirection = Vector3.right;
            }

            RaycastHit hit;

            if (!rb.SweepTest(moveDirection, out hit, swipeMove))
            {
                rb.MovePosition(rb.position + (moveDirection *
                                swipeMove));
            }
        }
    }

    public float minScale = 0.5f;

    public float maxScale = 3.0f;

    private float currentScale = 1;

    private void ScalePlayer()
    {
        if (Input.touchCount != 2)
        {
            return;
        }
        else
        { 
            Touch touch0 = Input.touches[0];
            Touch touch1 = Input.touches[1];

            Vector2 touch0Prev = touch0.position - touch0.deltaPosition;
            Vector2 touch1Prev = touch1.position - touch1.deltaPosition;

            float prevTouchDeltaMag = (touch0Prev - touch1Prev).magnitude;

            float touchDeltaMag = (touch0.position -
                                   touch1.position).magnitude;

            float deltaMagnitudeDiff = prevTouchDeltaMag - touchDeltaMag;

            float newScale = currentScale - (deltaMagnitudeDiff
                                             * Time.deltaTime);

            newScale = Mathf.Clamp(newScale, minScale, maxScale);

            transform.localScale = Vector3.one * newScale;

            currentScale = newScale;

        }
    }

    float CalculateMovement(Vector3 pixelPos)
    {
        var worldPos = Camera.main.ScreenToViewportPoint(pixelPos);

        float xMove = 0;

        if (worldPos.x < 0.5f)
        {
            xMove = -1;
        }
        else
        {
            xMove = 1;
        }

        return xMove * dodgeSpeed;
    }

    private static void TouchObjects(Touch touch)
    {
        Ray touchRay = Camera.main.ScreenPointToRay(touch.position);

        RaycastHit hit;

        int layerMask = ~0;
 
        if (Physics.Raycast(touchRay, out hit, Mathf.Infinity,
                            layerMask, QueryTriggerInteraction.Ignore))
        {
            hit.transform.SendMessage("PlayerTouch",
                                SendMessageOptions.DontRequireReceiver);
        }
    }

}