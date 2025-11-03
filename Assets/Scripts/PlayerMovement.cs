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
    public Collider2D ramp45Collider;         // il PolygonCollider2D SOLO della rampa +45
    public float stickToRamp = 3f;            // spinta verso la rampa per non staccarsi
    bool onRamp45;

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

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
    }

    public void FixedUpdate()
    {
        onRamp45 = Physics2D.IsTouching(playerCapsule, ramp45Collider);
        if (wallJumpPushTimer > 0f && horizontalMovement==0)
        {
            body.linearVelocityX = wallJumpDir * wallJumpPush * (1-(wallJumpPushDuration-wallJumpPushTimer)/wallJumpPushDuration);
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
            if (onRamp45)
                ApplyRamp45Motion();
        }
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

    private void ApplyRamp45Motion()
    {
        // tangente alla rampa +45: (1,1) normalizzata
        const float SQ2_2 = 0.70710678f; // 1/sqrt(2)
        Vector2 tangent = new Vector2(SQ2_2, SQ2_2);

        // normale "verso l'alto" per rampa +45: (-1, +1) normalizzata
        Vector2 rampNormal = new Vector2(-SQ2_2, SQ2_2);

        // velocit target lungo la tangente in base all'input orizzontale
        float speedAlong = horizontalMovement * playerSpeed;
        Vector2 tangVel = tangent * speedAlong;

        // piccolo stick per restare aderenti (spinta verso la rampa)
        Vector2 stickVel = -rampNormal * stickToRamp;

        Vector2 final = tangVel + stickVel;

        // evita di creare "spinta verso l'alto" quando cammini piano in salita
        if (final.y > 0f && Mathf.Abs(speedAlong) < playerSpeed * 0.2f)
            final.y = 0f;

        body.linearVelocityX = final.x;
        body.linearVelocityY = final.y;
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
