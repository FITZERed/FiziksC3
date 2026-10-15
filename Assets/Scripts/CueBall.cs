using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class CueBall : MonoBehaviour
{
    [Header("Scene references")]
    [SerializeField] private Camera aimingCamera;
    [SerializeField] private LineRenderer aimLine;
    [SerializeField] private Rigidbody[] balls;

    [Header("Shot settings")]
    [SerializeField] private float maximumDrag = 2f;
    [SerializeField] private float maximumImpulse = 1.2f;
    [SerializeField] private float minimumDrag = 0.05f;
    [SerializeField] private float maximumAngle = 5f;
    [SerializeField] private float stoppedSpeed = 0.05f;

    private Rigidbody body;
    private SphereCollider ballCollider;

    private bool aiming;
    private bool shotQueued;

    private Vector3 dragStart;
    private Vector3 clickOffset;
    private Vector3 shotDirection;
    private Vector3 queuedImpulse;

    private float shotPower;
    private float previewLength;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        ballCollider = GetComponent<SphereCollider>();

        if (aimingCamera == null)
            aimingCamera = Camera.main;

        if (aimLine != null)
        {
            aimLine.useWorldSpace = true;
            aimLine.positionCount = 2;
            aimLine.enabled = false;
        }
    }

    private void Update()
    {
        Mouse mouse = Mouse.current;

        if (mouse == null || aimingCamera == null)
            return;

        if (mouse.leftButton.wasPressedThisFrame &&
            !shotQueued && AllBallsStopped())
        {
            TryStartAiming(mouse.position.ReadValue());
        }

        if (!aiming)
            return;

        UpdateAim(mouse.position.ReadValue());

        if (mouse.leftButton.wasReleasedThisFrame)
        {
            aiming = false;

            if (aimLine != null)
                aimLine.enabled = false;

            if (shotPower > 0f)
            {
                queuedImpulse = shotDirection * shotPower;
                shotQueued = true;
            }
        }
    }

    private void FixedUpdate()
    {
        if (!shotQueued)
            return;

        body.AddForce(queuedImpulse, ForceMode.Impulse);
        shotQueued = false;
    }

    private void TryStartAiming(Vector2 screenPosition)
    {
        Ray ray = aimingCamera.ScreenPointToRay(screenPosition);

        if (!Physics.Raycast(ray, out RaycastHit hit))
            return;

        if (hit.collider != ballCollider)
            return;

        if (!GetMousePoint(screenPosition, out dragStart))
            return;

        clickOffset = dragStart - body.position;
        clickOffset.y = 0f;

        aiming = true;
        shotPower = 0f;
    }

    private void UpdateAim(Vector2 screenPosition)
    {
        if (!GetMousePoint(screenPosition, out Vector3 mousePoint))
        {
            shotPower = 0f;

            if (aimLine != null)
                aimLine.enabled = false;

            return;
        }

        Vector3 drag = mousePoint - dragStart;
        drag.y = 0f;

        float length = drag.magnitude;

        if (length < minimumDrag)
        {
            shotPower = 0f;

            if (aimLine != null)
                aimLine.enabled = false;

            return;
        }

        Vector3 direction = drag.normalized;

        // Measure the click offset sideways relative to this shot.
        Vector3 sideways = Vector3.Cross(Vector3.up, direction);

        float radius = ballCollider.radius *
            Mathf.Abs(transform.lossyScale.x);

        float offset = Vector3.Dot(clickOffset, sideways) /
            Mathf.Max(radius, 0.001f);

        float angle = Mathf.Clamp(offset, -1f, 1f) *
            maximumAngle;

        shotDirection =
            Quaternion.AngleAxis(angle, Vector3.up) * direction;

        previewLength = Mathf.Min(length, maximumDrag);

        shotPower = (previewLength / maximumDrag) *
            maximumImpulse;

        if (aimLine != null)
        {
            Vector3 start = body.position + Vector3.up * 0.12f;

            aimLine.enabled = true;
            aimLine.SetPosition(0, start);
            aimLine.SetPosition(
                1, start + shotDirection * previewLength);
        }
    }

    private bool GetMousePoint(
        Vector2 screenPosition, out Vector3 point)
    {
        Ray ray = aimingCamera.ScreenPointToRay(screenPosition);

        Plane aimingPlane = new Plane(Vector3.up, body.position);

        if (aimingPlane.Raycast(ray, out float distance))
        {
            point = ray.GetPoint(distance);
            return true;
        }

        point = Vector3.zero;
        return false;
    }

    private bool AllBallsStopped()
    {
        float limit = stoppedSpeed * stoppedSpeed;

        if (body.linearVelocity.sqrMagnitude > limit)
            return false;

        foreach (Rigidbody ball in balls)
        {
            if (ball != null &&
                ball.linearVelocity.sqrMagnitude > limit)
            {
                return false;
            }
        }

        return true;
    }
}