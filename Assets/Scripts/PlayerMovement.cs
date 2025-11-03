using UnityEngine;
using UnityEngine.Tilemaps;
using static UnityEngine.InputSystem.InputAction;

public class PlayerMovement : MonoBehaviour
{
    [Header("General Settings")]
    public float playerSpeed = 10;
    public float jumpForce = 10;

    [Header("Gravity Settings")]
    public float baseGravity = 2;
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
    public float wallJumpPushDuration = 0.1f; // quanto dura la spinta
    float wallJumpPushTimer = 0f;
    float wallJumpDir = 0f;                    // -1 sinistra, +1 destra

    [Header("Ramp +45 (dedicated collider)")]
    public CapsuleCollider2D playerCapsule;   // la tua capsule del player
    public Collider2D ramp_45Collider;         // il PolygonCollider2D SOLO della rampa +45
    public Collider2D ramp_m45Collider;         // il PolygonCollider2D SOLO della rampa -45
    const float SQ2_2 = 0.70710678f; // 1/sqrt(2)
    Vector2 tangent_45 = new Vector2(SQ2_2, SQ2_2);
    Vector2 tangent_m45 = new Vector2(-SQ2_2, SQ2_2);
    Vector2 tangent;
    bool onRamp_45, onRamp_m45;

    [Header("SFX")]
    public AudioClip JumpSFX;

    [Header("Components")]
    public Animator playerAnimator;
    public SpriteRenderer spriteRenderer;
    Rigidbody2D body;

    bool facingRight = true;
    public AudioSource audioSource;
    public float wallspan = 0.71f;

    float horizontalMovement = 0;

    
    bool jumpPressed = false;
    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
    }

    public void FixedUpdate()
    {
        onRamp_45 = Physics2D.IsTouching(playerCapsule, ramp_45Collider);
        onRamp_m45 = Physics2D.IsTouching(playerCapsule, ramp_m45Collider);
        if (wallJumpPushTimer > 0f && horizontalMovement == 0)
        {
            body.linearVelocityX = wallJumpDir * wallJumpPush * (1 - (wallJumpPushDuration - wallJumpPushTimer) / wallJumpPushDuration);
            wallJumpPushTimer -= Time.fixedDeltaTime;
            /*
            Debug.Log("Timer");
            Debug.Log(wallJumpPushTimer);
            Debug.Log("wallJumpDir");
            Debug.Log(wallJumpDir);
            */
        }
        else
        {
            body.linearVelocityX = horizontalMovement * playerSpeed;
            wallJumpPushTimer = 0f;
            if (onRamp_45 | onRamp_m45)
            {
                // velocit target lungo la tangente in base all'input orizzontale
                if (onRamp_45)
                    tangent = tangent_45;
                else
                    tangent = tangent_m45;

                Vector2 tangVel = tangent * body.linearVelocityX;
                Vector2 final = tangVel;

                body.linearVelocityX = final.x;
                if (!jumpPressed)
                    body.linearVelocityY = final.y;
            }
        }
        if (!isGrounded && jumpPressed)
            jumpPressed = false;
        GroundCheck();
        WallCheck();
        SetGravity();
    }

    public void Update()
    {
        playerAnimator.SetFloat("XSpeed", Mathf.Abs(body.linearVelocityX));
        playerAnimator.SetFloat("YSpeed", body.linearVelocityY);
        playerAnimator.SetBool("isGrounded", isGrounded);
        playerAnimator.SetBool("isOnWall", isOnWall);

        if (horizontalMovement > 0 && !facingRight)
        {
            Flip();
        }
        else if (horizontalMovement < 0 && facingRight)
        {
            Flip();
        }
    }

    private void SetGravity()
    {
        if (body.linearVelocityY < 0)
        {
            body.gravityScale = baseGravity * fallSpeedMultiplier;
            body.linearVelocityY = Mathf.Max(body.linearVelocityY, -maxFallSpeed);
        }
        else
        {
            body.gravityScale = baseGravity;
        }
    }

    public void GroundCheck()
    {
        Vector3 wp = groundCheckTransform.position;
        Vector3Int cell = groundTilemap.WorldToCell(wp);
        isGrounded = groundTilemap.HasTile(cell);
        if (onRamp_45)
            isGrounded = true;
    }

    public void WallCheck()
    {
        // Punto da controllare (leggermente sotto i piedi)
        Vector3 wp = wallCheckTransform.position;

        Vector3Int cell = groundTilemap.WorldToCell(wp);

        // Se la cella contiene un tile, il punto  "dentro il terreno"
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
                wallJumpDir = facingRight ? -1 : 1;
                wallJumpPushTimer = wallJumpPushDuration;
                Flip();
                audioSource.PlayOneShot(JumpSFX);
            }
        }

        if (context.canceled && body.linearVelocityY > 0)
        {
            body.linearVelocityY = body.linearVelocityY / 2;
        }

    }

    private void Flip()
    {
        Vector3 pos = wallCheckTransform.localPosition;
        if (facingRight)
        {
            pos.x -= wallspan;
        }
        else
        {
            pos.x += wallspan;
        }
        facingRight = !facingRight;
        spriteRenderer.flipX = !spriteRenderer.flipX;
        wallCheckTransform.localPosition = pos;
    }

    public void OnDrawGizmos()
    {
        //Gizmos.DrawCube(groundCheckTransform.position, groundCheckSize);
        //Gizmos.DrawCube(wallCheckTransform.position, wallCheckSize);
        Vector3 g = groundCheckTransform.position;
        Gizmos.DrawWireSphere(g, 0.03f);

        Vector3 w = wallCheckTransform.position;
        Gizmos.DrawWireSphere(w, 0.03f);
    }


}
