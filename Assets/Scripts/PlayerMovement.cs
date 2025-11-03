using UnityEngine;
using UnityEngine.Tilemaps;
using static UnityEngine.InputSystem.InputAction;

public class PlayerMovement : MonoBehaviour
{
    [Header("General Settings")]
    public float playerSpeed = 10f;
    public float jumpForce = 10f;

    [Header("Gravity Settings")]
    public float baseGravity = 2f;
    public float maxFallSpeed = 18f;
    public float fallSpeedMultiplier = 2f;

    [Header("Ground Check")]
    public Transform groundCheckTransform;
    public LayerMask groundLayer;
    public Tilemap groundTilemap;
    bool isGrounded;

    [Header("Wall Check")]
    public Transform wallCheckTransform;
    bool isOnWall;

    [Header("Wall Jump")]
    public float wallJumpPush = 10f;
    public float wallJumpPushDuration = 0.1f;   // durata spinta orizzontale
    float wallJumpPushTimer = 0f;
    float wallJumpDir = 0f;                     // -1 sinistra, +1 destra

    [Header("Ramp ±45 (dedicated colliders)")]
    public CapsuleCollider2D playerCapsule;     // capsule del player
    public LayerMask rampPlus45Layer;   // Layer: SlopePlus45
    public LayerMask rampMinus45Layer;  // Layer: SlopeMinus45
    const float SQ2_2 = 0.70710678f;            // 1/sqrt(2)
    static readonly Vector2 tangent45 = new Vector2(SQ2_2, SQ2_2);
    static readonly Vector2 tangentM45 = new Vector2(-SQ2_2, SQ2_2);
    bool onRamp45, onRampM45;

    [Header("SFX")]
    public AudioClip JumpSFX;

    [Header("Components")]
    public Animator playerAnimator;
    public SpriteRenderer spriteRenderer;
    public AudioSource audioSource;
    Rigidbody2D body;

    bool facingRight = true;
    public float wallspan = 0.71f;

    float horizontalMovement = 0f;
    bool jumpPressed = false;

    void Awake() => body = GetComponent<Rigidbody2D>();

    void FixedUpdate()
    {
        // Stato rampe
        onRamp45 = Physics2D.OverlapCapsule(playerCapsule.bounds.center, playerCapsule.size,
                                     CapsuleDirection2D.Vertical, 0f, rampPlus45Layer);
        onRampM45 = Physics2D.OverlapCapsule(playerCapsule.bounds.center, playerCapsule.size,
                                             CapsuleDirection2D.Vertical, 0f, rampMinus45Layer);


        // Spinta orizzontale post wall-jump se non c'è input
        if (wallJumpPushTimer > 0f && Mathf.Approximately(horizontalMovement, 0f))
        {
            float t = 1f - (wallJumpPushDuration - wallJumpPushTimer) / wallJumpPushDuration;
            body.linearVelocityX = wallJumpDir * wallJumpPush * t;
            wallJumpPushTimer -= Time.fixedDeltaTime;
        }
        else
        {
            body.linearVelocityX = horizontalMovement * playerSpeed;
            wallJumpPushTimer = 0f;
            ApplyRampMotion(); // proietta il movimento lungo la rampa se presente e non in salto
        }

        if (!isGrounded && jumpPressed) jumpPressed = false;

        GroundCheck();
        WallCheck();
        ApplyGravity();
    }

    void Update()
    {
        playerAnimator.SetFloat("XSpeed", Mathf.Abs(body.linearVelocityX));
        playerAnimator.SetFloat("YSpeed", body.linearVelocityY);
        playerAnimator.SetBool("isGrounded", isGrounded);
        playerAnimator.SetBool("isOnWall", isOnWall);

        if (horizontalMovement > 0f && !facingRight) Flip();
        else if (horizontalMovement < 0f && facingRight) Flip();
    }

    void ApplyGravity()
    {
        if (body.linearVelocityY < 0f)
        {
            body.gravityScale = baseGravity * fallSpeedMultiplier;
            body.linearVelocityY = Mathf.Max(body.linearVelocityY, -maxFallSpeed);
        }
        else
        {
            body.gravityScale = baseGravity;
        }
    }

    void ApplyRampMotion()
    {
        if (jumpPressed) return;                   // non alterare la traiettoria durante il salto
        if (!(onRamp45 || onRampM45)) return;

        if (onRamp45)
        {
            Vector2 v = tangent45 * body.linearVelocityX;
            body.linearVelocityX = v.x;
            body.linearVelocityY = v.y;
        }
        else // rampa -45
        {
            float sx = Mathf.Sign(body.linearVelocityX);
            Vector2 v = tangentM45 * Mathf.Abs(body.linearVelocityX);
            body.linearVelocityX = -sx * v.x;
            body.linearVelocityY = -sx * v.y;
        }
    }

    void GroundCheck()
    {
        Vector3Int cell = groundTilemap.WorldToCell(groundCheckTransform.position);
        isGrounded = groundTilemap.HasTile(cell) || onRamp45; // come nel codice originale
    }

    void WallCheck()
    {
        Vector3Int cell = groundTilemap.WorldToCell(wallCheckTransform.position);
        isOnWall = groundTilemap.HasTile(cell);
    }

    public void PlayerInput_Move(CallbackContext context)
    {
        horizontalMovement = context.ReadValue<Vector2>().x;
    }

    public void PlayerInput_Jump(CallbackContext context)
    {
        jumpPressed = true;

        if (isGrounded)
        {
            if (context.performed)
            {
                body.linearVelocityY = jumpForce;
                audioSource.PlayOneShot(JumpSFX);
            }
        }
        else if (isOnWall)
        {
            if (context.performed)
            {
                body.linearVelocityY = jumpForce;
                wallJumpDir = facingRight ? -1f : 1f;
                wallJumpPushTimer = wallJumpPushDuration;
                Flip();
                audioSource.PlayOneShot(JumpSFX);
            }
        }

        if (context.canceled && body.linearVelocityY > 0f)
        {
            body.linearVelocityY *= 0.5f;
        }
    }

    void Flip()
    {
        Vector3 pos = wallCheckTransform.localPosition;
        pos.x += facingRight ? -wallspan : wallspan; // sposta il punto di wall check
        facingRight = !facingRight;
        spriteRenderer.flipX = !spriteRenderer.flipX;
        wallCheckTransform.localPosition = pos;
    }

    void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(groundCheckTransform.position, 0.03f);
        Gizmos.DrawWireSphere(wallCheckTransform.position, 0.03f);
    }
}
