using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerBrain : MonoBehaviour
{
    // Variables

    [Header("Gravity")]
    public float fallMultiplier = 2.5f;
    public float lowJumpMultiplier = 2f;
    [Space(10)]

    [Header("Jumping")]
    public bool grounded = true;
    public bool touchingWall = false;
    [Space(5)]

    // Coyote Time
    private float coyoteTimeLeft = 0.3f;
    public float coyoteTimeMax = 0.3f;
    [Space(5)]

    // Jump
    private float jumpBufferLeft = 0.275f;
    public float jumpBufferMax = 0.275f;
    [Space(5)]

    [Header("Wall Jumping")]

    public float jumpOffSpeed = 1.5f;
    public bool wallJumped = false;
    [Space(5)]

    private float grabTimeLeft = 0.2f;
    public float grabTimeMax = 0.2f;
    [Space(5)]

    [Range(5,20)]
    public float jumpForce = 10;
    public float jumpLerp = 5;
    [Space(10)]
    
    [Header("Movement")]
    [Range(5,20)]
    public float speed = 10;
    [Range(0f,2f)]
    public float slideSpeed = 1f;
    [Range(1f,5f)]
    private string sideTouching = "none";
    private string lastSideTouching = "none";

    // Other
    SpriteRenderer playerSprite;
    Rigidbody2D rb;
    LayerMask groundLayerMask;

    void Start()
    {
        // Set the rigidbody on the player as "rb"
        rb = GetComponent<Rigidbody2D> ();

        // Gets the ground's layer and sets a var to it
        groundLayerMask = (LayerMask.GetMask("Ground"));

        playerSprite = GetComponentInChildren<SpriteRenderer>();
    }

    void Update()
    {

        // Get player input
        float moveX = Input.GetAxis("Horizontal");
        float moveY = Input.GetAxis("Vertical");

        // Create the movement vector based on player input
        Vector2 moveVector = new Vector2(moveX, moveY);

        // Call our movement functions
        playerMove(moveVector);
        flipSprite(moveX);
        playerJump();
        playerWall();
    }

    private void playerMove(Vector2 dir)
    {
        if(!wallJumped)
        {
            // Set the player's movement direction based on current player input + current vertical Y movement
            rb.velocity = (new Vector2(dir.x * speed, rb.velocity.y));
        }
        
        else
        {
            // If the player is wall jumping, change their movement so they have more in-air control using LERP.
            rb.velocity = Vector2.Lerp(rb.velocity, (new Vector2(dir.x * speed, rb.velocity.y)), jumpLerp * Time.deltaTime);
        }
        
    }

    private void playerJump()
    {
        checkGrounded();
        jumpGravityModulator();
        coyoteTime();
        jumpBuffer();

        // If you have coyote time and you jump (using the jump buffer)
        if(coyoteTimeLeft > 0f && jumpBufferLeft > 0f)
        {
            // Set velocity to a new vector
            rb.velocity = new Vector2(rb.velocity.x, 0);

            // Add force upward based on the jump force
            rb.velocity += Vector2.up * jumpForce;

            // Remove coyote time and the jump buffer so you can't double jump
            coyoteTimeLeft = 0f;
            jumpBufferLeft = 0f;
        }
    }

    private void playerWall()
    {
        checkWall();
        checkIfResetWall();
        grabTime();

        // If the player is touching the wall
        if(touchingWall && !grounded)
        {
            // If the player jumps and hasn't walljumped yet
            if(Input.GetButtonDown("Jump") && !wallJumped)
            {
                // If they are on the left wall
                if(sideTouching == "left") 
                {
                    // Shoot them off to the right
                    rb.velocity = new Vector2(speed * jumpOffSpeed, 0);
                    rb.velocity += Vector2.up * jumpForce;
                }

                // Otherwise
                else
                {
                    // Shoot them off to the left
                    rb.velocity = new Vector2(speed * jumpOffSpeed, 0);
                    rb.velocity += Vector2.up * jumpForce;
                }

                wallJumped = true;
                lastSideTouching = sideTouching;
            }

            // Otherwise
            else if(!grounded && Input.GetKey("left shift") && grabTimeLeft < 0)
            {
                // Make the player fall slower equal to the slide speed.
                rb.velocity = new Vector2(rb.velocity.x, -slideSpeed);
            }

        }
    }

    private void jumpGravityModulator()
    {
        // If the player is falling
        if (rb.velocity.y < 0) 
        {
            rb.gravityScale = fallMultiplier;
        }  
        
        // If the player is not holding jump and they are going upwards
        else if (rb.velocity.y > 0 && Input.GetAxis("Jump") == 0) 
        {
            rb.gravityScale = lowJumpMultiplier;
        }

        // Otherwise, gravity scale is normal.
        else
        {
            rb.gravityScale = 1f;
        }
    }

    private void jumpBuffer()
    {
        // If the player jumps (axis not equal to 0), reset their jump buffer.
        if(Input.GetButtonDown("Jump")) 
        {
            jumpBufferLeft = jumpBufferMax;
        }

        // Otherwise, start decreasing their jump buffer.
        else 
        {
            jumpBufferLeft -= Time.deltaTime;
        }
    }

    private void coyoteTime()
    {
        // If the player is grounded, reset Coyote Time.
        if(grounded) 
        {
            coyoteTimeLeft = coyoteTimeMax;
        }

        // Otherwise, start decreasing Coyote Time.
        else
        {
            coyoteTimeLeft -= Time.deltaTime;
        }
    }

    private void grabTime()
    {
        // If the player is grounded, reset Coyote Time.
        if(wallJumped) 
        {
            grabTimeLeft = grabTimeMax;
        }

        // Otherwise, start decreasing Coyote Time.
        else
        {
            grabTimeLeft -= Time.deltaTime;
        }
    }

    private void checkGrounded()
    {
        // Cast a ray downwards out .6u
        RaycastHit2D downwardRay = Physics2D.Raycast(transform.position, -Vector2.up, 0.6f, groundLayerMask);
        
        // If it hits something that is the ground
        if (downwardRay.collider != null)
        {
            // Set grounded to true
            grounded = true;
            // Also, reset the walljump.
            wallJumped = false;
        }
        else
        {
            // Otherwise, set it to false
            grounded = false;
        }
    }

    private void checkWall()
    {
        // Cast a ray sideways out .6u
        RaycastHit2D leftRay = Physics2D.Raycast(transform.position, Vector2.left, 0.55f, groundLayerMask);
        RaycastHit2D rightRay = Physics2D.Raycast(transform.position, Vector2.right, 0.55f, groundLayerMask);
        
        // If it hits something on the left that is the ground
        if (leftRay.collider != null)
        {
            // The player is touching the wall to true
            touchingWall = true;
            // Output result
            sideTouching = "left";
        }

        // If it hits something on the right that is the ground
        else if (rightRay.collider != null)
        {
            // The player is touching the wall to true
            touchingWall = true;
            // Output result
            sideTouching = "right";
        }

        // Otherwise, set it to false
        else
        {
            touchingWall = false;
        }
    }

    private void checkIfResetWall()
    {
        if (lastSideTouching != sideTouching && sideTouching != "none")
        {
            wallJumped = false;

        }
    }

    private void flipSprite(float moveX)
    {
        // If the moveX is negative
        if(moveX < 0f) 
        {
            // and if the player sprite is found
            if(playerSprite != null) 
            {
                // flip the sprite
                playerSprite.flipX = true;
            }
        }

        // Otherwise, if the moveX is positive
        else if (moveX > 0f) 
        {
            // if the player sprite is found
            if(playerSprite != null) 
            {
                // unflip the sprite
                playerSprite.flipX = false;
            }
        }
    }

}