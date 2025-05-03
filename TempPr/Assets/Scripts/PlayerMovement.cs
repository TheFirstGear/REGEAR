using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float dashSpeed = 15f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 1f;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float checkRadius = 0.2f;
    [SerializeField] private float crouchSpeedMultiplier = 0.5f;
    [SerializeField] private float crouchHeightMultiplier = 0.5f;
    [SerializeField] private Transform ceilingCheck;
    [SerializeField] private InputManager inputManager;
    [SerializeField] private Button jumpButton; // Workaround
    [SerializeField] private float hatchSpeed = 10f;
    [SerializeField] private float hatchMaxDistance = 10f;

    private Rigidbody2D rb;
    private CapsuleCollider2D collider;
    private float moveInput;
    private bool isGrounded;
    private bool isDashing;
    private bool canDash = true;
    private float lastMoveDirection = 1f;
    private LayerMask groundLayer;
    private bool isCrouching;
    private Vector2 originalColliderSize;
    private Vector2 originalColliderOffset;
    private Vector3 originalGroundCheckPos;
    private Vector3 originalLocalScale;
    private bool isHatching;
    private Vector2 hatchTarget;
    private Vector2 lastJoystickDirection = Vector2.right; // Default to right

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        collider = GetComponent<CapsuleCollider2D>();
        groundLayer = LayerMask.GetMask("Ground");
        originalColliderSize = collider.size;
        originalColliderOffset = collider.offset;
        originalGroundCheckPos = groundCheck.localPosition;
        originalLocalScale = transform.localScale;

        SetupJumpButton();
    }

    void SetupJumpButton()
    {
        if (jumpButton != null)
        {
            jumpButton.onClick.RemoveAllListeners();
            jumpButton.onClick.AddListener(OnJumpButtonClicked);
        }
    }

    void OnJumpButtonClicked()
    {
        if (isGrounded && !isCrouching && !isHatching)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }
    }

    void Update()
    {
        moveInput = inputManager.Horizontal;
        if (moveInput != 0)
        {
            lastMoveDirection = moveInput;
        }

        // Store last non-zero joystick direction
        Vector2 joystickInput = new Vector2(inputManager.Horizontal, inputManager.Vertical);
        if (joystickInput != Vector2.zero)
        {
            lastJoystickDirection = joystickInput.normalized;
        }

        isGrounded = Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundLayer);

        if (inputManager.IsJumpPressed && isGrounded && !isCrouching && !isHatching)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }

        if (inputManager.IsDashCrouchTapped && isGrounded && !isDashing && !isCrouching && canDash)
        {
            StartCoroutine(Dash());
        }

        if (inputManager.IsDashCrouchHeld && isGrounded && !isDashing && !isCrouching)
        {
            isCrouching = true;
            collider.size = new Vector2(originalColliderSize.x, originalColliderSize.y * crouchHeightMultiplier);
            collider.offset = new Vector2(originalColliderOffset.x, 0f);
            transform.localScale = new Vector3(originalLocalScale.x, originalLocalScale.y * crouchHeightMultiplier, originalLocalScale.z);
            float positionOffsetY = -(originalColliderSize.y * (1f - crouchHeightMultiplier)) / 2f;
            transform.position = new Vector3(transform.position.x, transform.position.y + positionOffsetY, transform.position.z);
            groundCheck.localPosition = originalGroundCheckPos;
        }
        else if (!inputManager.IsDashCrouchHeld && isCrouching)
        {
            bool ceilingHit = Physics2D.OverlapCircle(ceilingCheck.position, checkRadius, groundLayer);
            if (!ceilingHit)
            {
                isCrouching = false;
                collider.size = originalColliderSize;
                collider.offset = originalColliderOffset;
                transform.localScale = originalLocalScale;
                float positionOffsetY = (originalColliderSize.y * (1f - crouchHeightMultiplier)) / 2f;
                transform.position = new Vector3(transform.position.x, transform.position.y + positionOffsetY, transform.position.z);
                groundCheck.localPosition = originalGroundCheckPos;
            }
        }

        if (inputManager.IsHatchHeld && !isHatching && !isCrouching && !isDashing)
        {
            Vector2 hatchDirection = new Vector2(inputManager.Horizontal, inputManager.Vertical).normalized;
            if (hatchDirection == Vector2.zero)
            {
                hatchDirection = lastJoystickDirection;
            }

            RaycastHit2D hit = Physics2D.Raycast(transform.position, hatchDirection, hatchMaxDistance, groundLayer);
            if (hit.collider != null)
            {
                isHatching = true;
                hatchTarget = hit.point;
                rb.isKinematic = true;
            }
        }

        if (isHatching)
        {
            Vector2 newPosition = Vector2.MoveTowards(transform.position, hatchTarget, hatchSpeed * Time.deltaTime);
            transform.position = new Vector3(newPosition.x, newPosition.y, transform.position.z);

            if (Vector2.Distance(transform.position, hatchTarget) < 0.01f)
            {
                transform.position = new Vector3(hatchTarget.x, hatchTarget.y, transform.position.z);
                rb.linearVelocity = Vector2.zero;
            }

            if (!inputManager.IsHatchHeld)
            {
                isHatching = false;
                rb.isKinematic = false;
            }
        }
    }

    void FixedUpdate()
    {
        if (!isDashing && !isHatching)
        {
            float currentSpeed = isCrouching ? moveSpeed * crouchSpeedMultiplier : moveSpeed;
            rb.linearVelocity = new Vector2(moveInput * currentSpeed, rb.linearVelocity.y);
        }
    }

    private IEnumerator Dash()
    {
        canDash = false;
        isDashing = true;

        float horizontalInput = inputManager.Horizontal;
        float dashDirectionX = horizontalInput != 0 ? horizontalInput : lastMoveDirection;

        Vector2 dashVelocity = new Vector2(dashDirectionX * dashSpeed, 0f);
        rb.linearVelocity = dashVelocity;

        yield return new WaitForSeconds(dashDuration);

        isDashing = false;
        float currentSpeed = isCrouching ? moveSpeed * crouchSpeedMultiplier : moveSpeed;
        rb.linearVelocity = new Vector2(inputManager.Horizontal * currentSpeed, rb.linearVelocity.y);

        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    void OnDrawGizmos()
    {
        if (inputManager != null && inputManager.IsHatchHeld)
        {
            Vector2 direction = new Vector2(inputManager.Horizontal, inputManager.Vertical).normalized;
            if (direction == Vector2.zero)
                direction = lastJoystickDirection;
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, direction * hatchMaxDistance);
        }
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, checkRadius);
        }
    }
}