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
    public float totalAirTime = 0f;
    public float airTimeGripReq = 0.5f;
    [Space(5)]

    // Coyote Time
    private float coyoteTimeLeft = 0.3f;
    public float coyoteTimeMax = 0.3f;
    [Space(5)]

    // Jump
    private float jumpBufferLeft = 0.175f;
    public float jumpBufferMax = 0.175f;
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

    // Particles
    private GameObject slideLeft;
    private GameObject slideRight;
    private ParticleSystem slideLeftParticles;
    private ParticleSystem slideRightParticles;

    // Shields
    [Header("Shields")]
    public List<GameObject> shieldsList = new List<GameObject>();

    public string resetKey = "r";

    [Range(3f,8f)]
    public float shieldThrowDistance = 4.0f;
    private int shieldsOut = 0;

    // SFX
    private AudioClip shieldthrow;
    private AudioClip jumpSound;
    private AudioClip groundLand;

    // Other
    private bool switchCase = false;
    SpriteRenderer playerSprite;
    Rigidbody2D rb;
    LayerMask groundLayerMask;
    LayerMask shieldLayerMask;
    LayerMask floorLayerMask;
    AudioSource audioSource;
    Animator animator;

    void Start()
    {
        // Set the rigidbody on the player as "rb"
        rb = GetComponent<Rigidbody2D> ();

        // Gets the ground's layer and sets a var to it
        groundLayerMask = (LayerMask.GetMask("Ground"));
        floorLayerMask =~ (LayerMask.GetMask("Player"));
        shieldLayerMask = (LayerMask.GetMask("Shield"));

        // Particle Systems
        slideLeft = GameObject.Find("Sliding Left");
        slideRight = GameObject.Find("Sliding Right");
        slideLeftParticles = slideLeft.GetComponent<ParticleSystem>();
        slideRightParticles = slideRight.GetComponent<ParticleSystem>();

        // Gets the sprite to flip
        playerSprite = GetComponentInChildren<SpriteRenderer>();

        // Set Audiosource
        audioSource = GetComponent<AudioSource>();

        // Set Shield List
        foreach(GameObject shieldID in GameObject.FindGameObjectsWithTag("Shield")) 
        {
             shieldsList.Add(shieldID);
        }

        print("Shields found! # of shields: " + shieldsList.Count);

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
        walkAnim(moveX);
        jumpAnim();
        playerJump(moveVector);
        checkAirTime();
        playerWall(moveVector);
        throwShield();
        

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
            rb.velocity = (new Vector2(dir.x * speed, 0));
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

            // Otherwise, if a player isn't grounded and they arent just in the air
            else if(!grounded && sideTouching != "none")
            {
                // Make the player fall slower equal to the slide speed.

                // If touching the left side, make the particles appear on that side.
                if(sideTouching == "left" && rb.velocity.x < 0 && totalAirTime > airTimeGripReq)
                {
                    rb.velocity = new Vector2(rb.velocity.x, -slideSpeed);
                    slideLeftParticles.Play();
                    slideRightParticles.Pause();
                }

                // Otherwise they appear on the right
                else if(sideTouching == "right" && rb.velocity.x > 0 && totalAirTime > airTimeGripReq)
                {
                    rb.velocity = new Vector2(rb.velocity.x, -slideSpeed);
                    slideRightParticles.Play();
                    slideLeftParticles.Pause();
                }
            }
        }

        // If nothing is happening here, disable particle systems as a just in case scenario.
        else
        {
            slideLeftParticles.Pause();
            slideRightParticles.Pause();
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

    private void checkAirTime()
    {
        // If the player is on the ground/is touching a wall after wall jumping/walljumping, reset their air time
        if(grounded || sideTouching == "none" && wallJumped)
        {
            totalAirTime = 0;
        }

        // Otherwise, start decreasing their jump buffer.
        else 
        {
            totalAirTime += Time.deltaTime;
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
        RaycastHit2D downwardRayLeft = Physics2D.Raycast(transform.position + new Vector3(-0.35f, -0.9f, 0), -Vector2.up, 0.1f, floorLayerMask);
        RaycastHit2D downwardRayRight = Physics2D.Raycast(transform.position + new Vector3(0.35f, -0.9f, 0), -Vector2.up, 0.1f, floorLayerMask);

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
            sideTouching = "none";
        }
    }

    private void checkIfResetWall()
    {
        // If the player touches another wall that wasn't the same as the wall they jumped off of, let them walljump again
        if (sideTouching != "none" && sideTouching != lastSideTouching)
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

    private void walkAnim(float moveX)
    {
        // If the moveX is negative or positive
        if (moveX < 0f || moveX > 0f)
        {
            // set animation
            animator = this.GetComponentInChildren<Animator>();
            animator.SetBool("walking", true);
        }

        // Otherwise, if the movex is 0
        else if (moveX == 0f)
        {
            // set animation
            animator = this.GetComponentInChildren<Animator>();
            animator.SetBool("walking", false);
        }
    }

    private void jumpAnim()
    {
        // If not grounded, set jump animation
        if (!grounded)
        {
            // set animation
            animator = this.GetComponentInChildren<Animator>();
            animator.SetInteger("jump", 1);
        }

        // if falling, set fall animation
        if (!grounded && rb.velocity.y < 0f)
        {
            // set animation
            animator = this.GetComponentInChildren<Animator>();
            animator.SetInteger("jump", 2);
        }

        // landing
        if (grounded)
        {
            // set animation
            animator = this.GetComponentInChildren<Animator>();
            animator.SetInteger("jump", 0);
        }
    }

    private void throwShield()
    {
        if(Input.GetAxis("ShieldThrowKeys") != 0 && shieldsOut != shieldsList.Count - 1)
        {
            // Get Player Y to set to shield.
            float shieldX = 0;
            float shieldY = this.transform.position.y;

            // Defaults to left if a null gets thrown
            string shieldD = "Left";

            // Cast a ray out on both sides, because fuck.
            RaycastHit2D leftShieldRay = Physics2D.Raycast(transform.position, Vector2.left, shieldThrowDistance, groundLayerMask);
            RaycastHit2D rightShieldRay = Physics2D.Raycast(transform.position, Vector2.right, shieldThrowDistance, groundLayerMask);

            if(Physics2D.Raycast(transform.position, Vector2.left, shieldThrowDistance, shieldLayerMask) && Input.GetAxis("ShieldThrowKeys") == -1 && !switchCase
            || Physics2D.Raycast(transform.position, Vector2.right, shieldThrowDistance, shieldLayerMask) && Input.GetAxis("ShieldThrowKeys") == 1 && !switchCase)
            {
                switchCase = true;
                print("owned");
            }

            // Get the player sprite direction, shoot the ray in the direction that they're facing
            else if(Input.GetAxis("ShieldThrowKeys") == -1 && leftShieldRay && !switchCase)
            {
                // Sets the Shield's X to the distance from the player +/- 1 so the shield doesn't clip in the wall.
                shieldX = this.transform.position.x - leftShieldRay.distance + .5f;
                print("Found an object - distance: " + leftShieldRay.distance);
                shieldD = "Left";
                spawnShield(shieldX, shieldY, shieldD);
                audioSource.PlayOneShot(shieldthrow, 0.6F);
                switchCase = true;

            }

            // Get the player sprite direction, shoot the ray in the direction that they're facing
            else if(Input.GetAxis("ShieldThrowKeys") == 1 && rightShieldRay && !switchCase)
            {
                // Sets the Shield's X to the distance from the player +/- 1 so the shield doesn't clip in the wall.
                shieldX = this.transform.position.x + rightShieldRay.distance - .5f;
                print("Found an object - distance: " + rightShieldRay.distance);
                shieldD = "Right";
                spawnShield(shieldX, shieldY, shieldD);
                audioSource.PlayOneShot(shieldthrow, 0.6F);
                switchCase = true;
            }

            // Get the player sprite direction, puts the shield out as far as the ray is cast
            else if(!switchCase)
            {
                // If nothing is found, set it out the ray distance.
                if(Input.GetAxis("ShieldThrowKeys") == -1)
                {
                    shieldX = this.transform.position.x - shieldThrowDistance + .5f;
                    shieldD = "Left";
                    switchCase = true;
                }
                else if(Input.GetAxis("ShieldThrowKeys") == 1)
                {
                    shieldX = this.transform.position.x + shieldThrowDistance - .5f;
                    shieldD = "Right";
                    switchCase = true;
                }
                print("No object found!");
                spawnShield(shieldX, shieldY, shieldD);
                audioSource.PlayOneShot(shieldthrow, 0.6F);
            }
        }

        if(Input.GetAxis("ShieldThrowKeys") == 0)
        {
            switchCase = false;
        }

        if(Input.GetKeyDown(resetKey))
        {
            foreach (GameObject shieldID in shieldsList)
            {
                shieldID.transform.position = new Vector2(0, -50.0f);
            }

            shieldsOut = 0;
        }
    }

    private void spawnShield(float shieldX, float shieldY, string shieldD)
    {
        /*
        This function spawns shields from a list set in Start
        !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                    ALL SHIELDS MUST BE ACTIVE AT GAME START OR IT WONT BE ADDED TO THE LIST
        !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        */

        if (shieldsList[shieldsOut] != null  && shieldsOut != shieldsList.Count - 1)
        {
            shieldsList[shieldsOut].SetActive(true);
            shieldsList[shieldsOut].transform.position = new Vector3(shieldX, shieldY, 0);
            animator = shieldsList[shieldsOut].GetComponentInChildren<Animator>();
            animator.SetTrigger(shieldD);
            shieldsOut += 1;

            print("Shield thrown: " + shieldsOut);
        }
        
        else
        {
            print("Player is out of shields!");
        }
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