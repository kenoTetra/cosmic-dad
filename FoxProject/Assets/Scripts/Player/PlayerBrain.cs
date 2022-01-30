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
    public bool canDoubleJump = false;
    [Space(5)]

    [Header("Wall Jumping")]

    public float jumpOffSpeed = 1.5f;
    public bool wallJumped = false;
    private bool wallJumping = false;
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
    [Range(20f,25f)]
    public float clampNumber = 23f;
    [Space(10)]

    // Other
    SpriteRenderer playerSprite;
    Rigidbody2D rb;
    LayerMask groundLayerMask;
    LayerMask shieldLayerMask;
    AudioSource audioSource;

    // Particles
    public ParticleSystem SlideLeftParticles;
    public ParticleSystem SlideRightParticles;
    [Space(10)]

    // Shields
    [Header("Shields")]
    public GameObject shieldOne;
    public GameObject shieldTwo;
    public GameObject shieldThree;
    public GameObject shieldFour;

    public string shieldKey = "e";
    [Range(3f,8f)]
    public float shieldThrowDistance = 4.0f;
    private int shieldsOut = 0;

    // SFX
    public AudioClip shieldthrow;
    public AudioClip jumpSound;
    public AudioClip groundLand;

    void Start()
    {
        // Set the rigidbody on the player as "rb"
        rb = GetComponent<Rigidbody2D> ();

        // Gets the ground's layer and sets a var to it
        groundLayerMask =~ (LayerMask.GetMask("Player"));
        shieldLayerMask = (LayerMask.GetMask("Shield"));

        // Particle Systems
        SlideLeftParticles.GetComponent<ParticleSystem>();
        SlideRightParticles.GetComponent<ParticleSystem>();

        // Gets the sprite to flip
        playerSprite = GetComponentInChildren<SpriteRenderer>();

        // Set Audiosource
        audioSource = GetComponent<AudioSource>();

        //Load Sound Effects
        shieldthrow = (AudioClip)Resources.Load("SFX/shieldthrow");
        jumpSound = (AudioClip)Resources.Load("SFX/jump");
        groundLand = (AudioClip)Resources.Load("SFX/groundland");
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
        playerJump(moveVector);
        playerWall(moveVector);
        throwShield(shieldKey);

        // Stop the floor despawning
        clampVelocity(clampNumber);
    }

    // Checks for triggers
    void OnTriggerEnter2D(Collider2D col)
    {
        if (col.gameObject.CompareTag("Wing")) 
        {
            print("Wing collected! " + col.gameObject.name);
            col.gameObject.SetActive(false);
            canDoubleJump = true;
        }
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

    private void playerJump(Vector2 dir)
    {
        checkGrounded();
        jumpGravityModulator();
        coyoteTime();
        jumpBuffer();
        checkDoubleJump();
        
        // If you have coyote time and you jump (using the jump buffer)
        if(coyoteTimeLeft > 0f && jumpBufferLeft > 0f && grounded)
        {
            // Set velocity to a new vector
            rb.velocity = (new Vector2(dir.x * speed, rb.velocity.y));

            // Add force upward based on the jump force
            rb.velocity += Vector2.up * jumpForce;

            //Play jump sound
            audioSource.PlayOneShot(jumpSound, 0.7F);

            // Remove coyote time and the jump buffer so you can't double jump
            coyoteTimeLeft = 0f;
            jumpBufferLeft = 0f;
        }

        // If you can double jump but can't jump normally and you're not touching something to walljump
        else if(canDoubleJump && Input.GetButtonDown("Jump") && sideTouching == "none")
        {
            // Double jump time baby
            rb.velocity = (new Vector2(dir.x * speed, rb.velocity.y));
            rb.velocity += Vector2.up * jumpForce;

            // Remove the double jump flag so no triple jumps
            canDoubleJump = false;
        }
        
    }

    private void playerWall(Vector2 dir)
    {
        checkWall();
        checkIfResetWall();

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
                    // Play jump sound
                    audioSource.PlayOneShot(jumpSound, 0.6F);
                    // Sets a split-second walljumping var for doublejump checks
                    wallJumping = true;
                }

                // Otherwise
                else
                {
                    // Shoot them off to the left
                    rb.velocity = new Vector2(speed * -jumpOffSpeed, 0);
                    rb.velocity += Vector2.up * jumpForce;
                    // Play jump sound
                    audioSource.PlayOneShot(jumpSound, 0.6F);
                    // Sets a split-second walljumping var for doublejump checks
                    wallJumping = true;
                }

                // Closeup var changes
                wallJumped = true;
                lastSideTouching = sideTouching;
                wallJumping = false;
            }

            // Otherwise, if a player hits left shift to slide
            else if(!grounded && Input.GetKey("left shift"))
            {
                // Make the player fall slower equal to the slide speed.
                rb.velocity = new Vector2(rb.velocity.x, -slideSpeed);

                // If touching the left side, make the particles appear on that side.
                if(sideTouching == "left")
                {
                    SlideLeftParticles.Play();
                    SlideRightParticles.Pause();
                }

                // Otherwise they appear on the right
                else
                {
                    SlideRightParticles.Play();
                    SlideLeftParticles.Pause();
                }
            }
        }

        // If nothing is happening here, disable particle systems as a just in case scenario.
        else
        {
            SlideLeftParticles.Pause();
            SlideRightParticles.Pause();
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

    private void checkGrounded()
    {
        // Cast rays downwards at either side of player out 0.1u
        RaycastHit2D downwardRayLeft = Physics2D.Raycast(transform.position + new Vector3(-0.35f, -0.9f, 0), -Vector2.up, 0.1f, groundLayerMask);
        RaycastHit2D downwardRayRight = Physics2D.Raycast(transform.position + new Vector3(0.35f, -0.9f, 0), -Vector2.up, 0.1f, groundLayerMask);

        // If they hit something that is the ground
        if (downwardRayLeft.collider != null || downwardRayRight.collider != null)
        {
            //Play landing sound once
            if (grounded == false)
            {
                audioSource.PlayOneShot(groundLand, 0.5F);
            }
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
        // If the player touches another wall that wasn't the same as the wall they jumped off of, let them walljump again
        // Easily modifiable to just be if they touch a wall by removing "lastSideTouching != sideTouching"
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

    private void throwShield(string shieldKey)
    {
        if(Input.GetKeyDown(shieldKey))
        {
            // Get Player Y to set to shield.
            float shieldX = 0;
            float shieldY = this.transform.position.y;

            RaycastHit2D leftShieldRay = Physics2D.Raycast(transform.position, Vector2.left, shieldThrowDistance, groundLayerMask);
            RaycastHit2D rightShieldRay = Physics2D.Raycast(transform.position, Vector2.right, shieldThrowDistance, groundLayerMask);

            if(Physics2D.Raycast(transform.position, Vector2.left, shieldThrowDistance, shieldLayerMask) && playerSprite.flipX || Physics2D.Raycast(transform.position, Vector2.right, shieldThrowDistance, shieldLayerMask) && !playerSprite.flipX)
            {
                print("owned");
            }

            // Get the player sprite direction, shoot the ray in the direction that they're facing
            else if(playerSprite.flipX && leftShieldRay)
            {
                // Sets the Shield's X to the distance from the player +/- 1 so the shield doesn't clip in the wall.
                shieldX = this.transform.position.x - leftShieldRay.distance + .5f;
                print("Found an object - distance: " + leftShieldRay.distance);
                spawnShield(shieldX, shieldY);
                audioSource.PlayOneShot(shieldthrow, 0.6F);

            }

            // Get the player sprite direction, shoot the ray in the direction that they're facing
            else if(!playerSprite.flipX && rightShieldRay)
            {
                // Sets the Shield's X to the distance from the player +/- 1 so the shield doesn't clip in the wall.
                shieldX = this.transform.position.x + rightShieldRay.distance - .5f;
                print("Found an object - distance: " + rightShieldRay.distance);
                spawnShield(shieldX, shieldY);
                audioSource.PlayOneShot(shieldthrow, 0.6F);
            }

            // Get the player sprite direction, puts the shield out as far as the ray is cast
            else
            {
                // If nothing is found, set it out the ray distance.
                if(playerSprite.flipX)
                {
                    shieldX = this.transform.position.x - shieldThrowDistance + .5f;
                }
                else
                {
                    shieldX = this.transform.position.x + shieldThrowDistance - .5f;
                }
                print("No object found!");
                spawnShield(shieldX, shieldY);
                audioSource.PlayOneShot(shieldthrow, 0.6F);
            }
        }
    }

    private void spawnShield(float shieldX, float shieldY)
    {
        /*
        This function spawns shields based on how many are active.
        EX:
        If none are activate it activates a new one.
        If one is active, it activates a new one.
        If two are active it moves the oldest one.
        Then, it activates a new one.
        */

        switch(shieldsOut)
        {
            case 0:
                shieldOne.SetActive(true);
                shieldOne.transform.position = new Vector3(shieldX, shieldY, 0);
                shieldsOut += 1;
                break;
            
            case 1:
                shieldTwo.SetActive(true);
                shieldTwo.transform.position = new Vector3(shieldX, shieldY, 0);
                shieldsOut += 1;
                break;

            case 2:
                shieldThree.SetActive(true);
                shieldThree.transform.position = new Vector3(shieldX, shieldY, 0);
                shieldsOut += 1;
                break;

            case 3:
                shieldFour.SetActive(true);
                shieldFour.transform.position = new Vector3(shieldX, shieldY, 0);
                shieldsOut = 0;
                break;
        }

        print("Shield thrown: " + shieldsOut);
    }

    private void checkDoubleJump()
    {
        // Removes doublejump when you touch the ground or a wall
        if(grounded || wallJumping)
        {
            canDoubleJump = false;
        }
    }

    private void clampVelocity(float clampNumber)
    {
        if(this.rb.velocity.y > clampNumber)
        {
            rb.velocity = new Vector2(rb.velocity.x, clampNumber);
        }

        if(this.rb.velocity.y < -clampNumber)
        {
            rb.velocity = new Vector2(rb.velocity.x, -clampNumber);
        }
    }
}