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
    public float odysseyJump = 12;
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

    /*
    Old Slide particle systems.
    private GameObject slideLeft;
    private GameObject slideRight;
    private ParticleSystem slideLeftParticles;
    private ParticleSystem slideRightParticles;
    */

    // Shields
    [Header("Shields")]
    public List<GameObject> shieldsList = new List<GameObject>();
    private int timesShieldsIncreased = 0;
    private bool shieldThrownInAir = false;
    public string resetKey = "r";

    [Range(3f,8f)]
    public float shieldThrowDistance = 4.0f;
    //public float shieldThrowbackY = 3f;
    //public float shieldThrowbackXMult = 1f;
    //private float shieldThrowbackX = 0f;
    [HideInInspector]
    public int shieldsOut = 0;

    // Reset Buffer for Shields
    private float resetHoldMax;
    private float resetCurrent;

    // SFX
    private AudioClip shieldthrow;
    private AudioClip jumpSound;
    private AudioClip groundLand;
    private AudioClip death;
    private AudioClip reload;
    private AudioClip breadGrab;
    private AudioClip checkpointLit;

    // HUD
    [HideInInspector]
    public float currentResetTime = 1f;

    // Death Checker
    public bool playerNaenae = false;

    // Loadzone Hit Check
    public bool loadZoneHit = false;

    // Checkers for Stats
    public int deathCount;
    public int shieldsThrownCount;
    public int jumpCount;
    public int wallJumpCount;

    // Timer for Stats
    public float time;
    private float msec;
    private float sec;
    private float min;

    // Other
    private bool switchCase = false;
    private float shieldSize = 1.5f;
    SpriteRenderer playerSprite;
    Rigidbody2D rb;
    LayerMask groundLayerMask;
    LayerMask shieldLayerMask;
    LayerMask floorLayerMask;
    AudioSource audioSource;
    Animator animator;
    Coroutine lastCoroutine = null;
    public GameObject currentCheckpoint;

    void Start()
    {
        // Set the rigidbody on the player as "rb"
        rb = GetComponent<Rigidbody2D> ();

        // Gets the ground's layer and sets a var to it
        groundLayerMask = (LayerMask.GetMask("Ground"));
        floorLayerMask = (LayerMask.GetMask("Ground", "Shield"));
        shieldLayerMask = (LayerMask.GetMask("Shield"));

        /* Particle Systems for sliding.
        slideLeft = GameObject.Find("Sliding Left");
        slideRight = GameObject.Find("Sliding Right");
        slideLeftParticles = slideLeft.GetComponent<ParticleSystem>();
        slideRightParticles = slideRight.GetComponent<ParticleSystem>();
        */

        // Sets stats values
        jumpCount = PlayerPrefs.GetInt("jumps");
        wallJumpCount = PlayerPrefs.GetInt("walljumps");
        shieldsThrownCount = PlayerPrefs.GetInt("shieldsThrown");
        deathCount = PlayerPrefs.GetInt("deaths");


        // Gets the sprite to flip
        playerSprite = GetComponentInChildren<SpriteRenderer>();

        // Set Audiosource
        audioSource = GetComponent<AudioSource>();

        // Set Shield List
        addToList("Shield", shieldsList);

        print("Shields found! # of shields: " + shieldsList.Count);

        // Set the shield size dependent on the scale X/2
        if (shieldsList[0] != null)
        {
            shieldSize = shieldsList[0].transform.localScale.x;
        }

        //Load Sound Effects
        shieldthrow = (AudioClip)Resources.Load("SFX/shieldthrow");
        jumpSound = (AudioClip)Resources.Load("SFX/jump");
        groundLand = (AudioClip)Resources.Load("SFX/groundland");
        death = (AudioClip)Resources.Load("SFX/death");
        reload = (AudioClip)Resources.Load("SFX/recall");
        breadGrab = (AudioClip)Resources.Load("SFX/breadgrab");
        checkpointLit = (AudioClip)Resources.Load("SFX/candle");
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
        playerJump(moveVector);
        jumpAnim();
        checkAirTime();
        playerWall(moveVector);
        throwShield();
        

        // Stop the floor despawning
        clampVelocity(clampNumber);
    }

    // Checks for triggers
    void OnTriggerEnter2D(Collider2D col)
    {
        // If the player collects a double jump wing, let them double jump.
        if (col.gameObject.CompareTag("Wing")) 
        {
            print("Wing collected! " + col.gameObject.name);
            col.gameObject.SetActive(false);
            canDoubleJump = true;
        }

        // checkpoints - hit one, it works!
        if (col.gameObject.CompareTag("Checkpoint"))
        {
            if(currentCheckpoint != null)
            {
                animator = currentCheckpoint.GetComponentInChildren<Animator>();
                animator.SetBool("activated", false);
            }

            if(currentCheckpoint != col.gameObject)
            {
                playerNaenae = true;
            }

            print("Checkpoint hit: " + col.gameObject.name);
            currentCheckpoint = col.gameObject;
            animator = col.gameObject.GetComponentInChildren<Animator>();
            if (animator.GetBool("activated") == false)
            {
                audioSource.PlayOneShot(checkpointLit, 0.7F);
            }
            animator.SetBool("activated", true);
        }

        // Spikes - hit one and die
        if (col.gameObject.CompareTag("Hazard"))
        {
            print ("Hazard hit! " + col.gameObject.name);
            this.transform.position = currentCheckpoint.transform.position;
            playerNaenae = true;
            audioSource.PlayOneShot(death, 0.7F);
            deathCount += 1;
            PlayerPrefs.SetInt("deaths", deathCount);
        }

        // Walk into a TriggerShieldsIncrease zone is a one time use--
        // Increases your shield count by however many shields are active in each tag: ShieldsIncrease1, ShieldsIncrease2...
        if (col.gameObject.CompareTag("TriggerShieldsIncrease"))
        {
            if(timesShieldsIncreased == 2) 
            {
                addToList("ShieldsIncrease3", shieldsList);
                audioSource.PlayOneShot(breadGrab, 1.2F);
                timesShieldsIncreased += 1;
            }

            else if(timesShieldsIncreased == 1)
            {
                addToList("ShieldsIncrease2", shieldsList);
                audioSource.PlayOneShot(breadGrab, 1.2F);
                timesShieldsIncreased += 1;
            }

            else
            {
                addToList("ShieldsIncrease1", shieldsList);
                audioSource.PlayOneShot(breadGrab, 1.2F);
                timesShieldsIncreased += 1;
            }

            col.gameObject.SetActive(false);
        }

        // Hit a ResetAllShields to remove all added shields.
        if (col.gameObject.CompareTag("ResetAllShields"))
        {
            shieldsList.Clear();
            addToList("Shield", shieldsList);
            timesShieldsIncreased = 0;
            col.gameObject.SetActive(false);
        }

        // Hit a loadzone to advance level.
        if (col.gameObject.CompareTag("Loadzone"))
        {
            loadZoneHit = true;
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
            jumpCount += 1;
            PlayerPrefs.SetInt("jumps", jumpCount);
        }

        // If you can double jump but can't jump normally and you're not touching something to walljump
        else if(canDoubleJump && Input.GetButtonDown("Jump") && sideTouching == "none")
        {
            // Double jump time baby
            rb.velocity = (new Vector2(dir.x * speed, 0));
            rb.velocity += Vector2.up * jumpForce;

            // Remove the double jump flag so no triple jumps
            jumpCount += 1;
            PlayerPrefs.SetInt("jumps", jumpCount);
            canDoubleJump = false;
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
                    // Play squish animation
                    animator = this.GetComponentInChildren<Animator>();
                    animator.SetTrigger("jumpsquish");
                    // Sets a split-second walljumping var for doublejump checks
                    wallJumpCount += 1;
                    PlayerPrefs.SetInt("walljumps", wallJumpCount);
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
                    // Play squish animation
                    animator = this.GetComponentInChildren<Animator>();
                    animator.SetTrigger("jumpsquish");
                    // Sets a split-second walljumping var for doublejump checks
                    wallJumpCount += 1;
                    PlayerPrefs.SetInt("walljumps", wallJumpCount);
                    wallJumping = true;
                }

                // Closeup var changes
                wallJumped = true;
                lastSideTouching = sideTouching;
                wallJumping = false;
            }

            /* FUCK SLIDING ALL MY HOMIES HATE SLIDING 
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
            */
        }

        /* fuck off sliding particles
        // If nothing is happening here, disable particle systems as a just in case scenario.
        else
        {
            slideLeftParticles.Pause();
            slideRightParticles.Pause();
        }
        */
    }

    private void jumpGravityModulator()
    {
        // If the player is falling
        if (rb.velocity.y < 0) 
        {
            rb.gravityScale = fallMultiplier;
        }  
        
        // If the player is not holding jump and they are going upwards OR they've thrown a shield in air...
        else if (rb.velocity.y > 0 && Input.GetAxis("Jump") == 0 || shieldThrownInAir == true) 
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
            //Play landing sound and animation once
            if (grounded == false)
            {
                audioSource.PlayOneShot(groundLand, 0.3F);
                // set animation
                animator = this.GetComponentInChildren<Animator>();
                animator.SetInteger("jump", 0);
            }
            // Set grounded to true
            grounded = true;
            // Also, reset the walljump.
            wallJumped = false;
            // Also, reset the gravity destroyer for in-air shield jumps.
            shieldThrownInAir = false;
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

    private void throwShield()
    {
        if(Input.GetAxis("ShieldThrowKeys") != 0 && shieldsOut != shieldsList.Count - 1)
        {
            // Cast a ray out on both sides, because fuck.
            RaycastHit2D leftShieldRay = Physics2D.Raycast(transform.position, Vector2.left, shieldThrowDistance, groundLayerMask);
            RaycastHit2D rightShieldRay = Physics2D.Raycast(transform.position, Vector2.right, shieldThrowDistance, groundLayerMask);

            // Cancel the throw if they'd overlap
            if(Physics2D.Raycast(transform.position, Vector2.left, shieldThrowDistance, shieldLayerMask) && Input.GetAxis("ShieldThrowKeys") == -1 && !switchCase
            || Physics2D.Raycast(transform.position, Vector2.right, shieldThrowDistance, shieldLayerMask) && Input.GetAxis("ShieldThrowKeys") == 1 && !switchCase)
            {
                switchCase = true;
                print("owned");
            }

            // If you shoot it out left, it goes left. EZ mode
            else if(Input.GetAxis("ShieldThrowKeys") == -1 && leftShieldRay && !switchCase)
            {
                shieldHit("Left", leftShieldRay);
            }

            // If you shoot it our right, it goes right.
            else if(Input.GetAxis("ShieldThrowKeys") == 1 && rightShieldRay && !switchCase)
            {
                shieldHit("Right", rightShieldRay);
            }

            // If nothing is found, set it out the ray distance.
            else if(Input.GetAxis("ShieldThrowKeys") == -1 && !switchCase)
            {
                shieldNoHit("Left");
            }

            else if(Input.GetAxis("ShieldThrowKeys") == 1 && !switchCase)
            {
                shieldNoHit("Right");
            }
        }

        // Using an axis as a button requires a switch case, if the button isn't being pressed
        // anymore, return the switch case back to false so we can use it again.
        if(Input.GetAxis("ShieldThrowKeys") == 0)
        {
            switchCase = false;
        }

        shieldResetChecker();
    }

    private void shieldResetChecker()
    {
        RaycastHit2D downwardRayLeft = Physics2D.Raycast(transform.position + new Vector3(-0.35f, -0.9f, 0), -Vector2.up, 0.1f, groundLayerMask);
        RaycastHit2D downwardRayRight = Physics2D.Raycast(transform.position + new Vector3(0.35f, -0.9f, 0), -Vector2.up, 0.1f, groundLayerMask);

        // If the reset key is held, start the shield reset coroutine
        if(Input.GetKeyDown(resetKey) && downwardRayLeft.collider != null || Input.GetKeyDown(resetKey) && downwardRayRight.collider != null)
        {
            lastCoroutine = StartCoroutine(shieldReset());
        }

        // If you release OR release before the coroutine is finished, stop the coroutine.
        if(Input.GetKey(resetKey) && currentResetTime <= 0 || Input.GetKeyUp(resetKey))
        {
            StopCoroutine(lastCoroutine);
            currentResetTime = 1f;
        }

        // OR if the player dies.
        if(playerNaenae)
        {
            foreach (GameObject shieldID in shieldsList)
            {
                shieldID.transform.position = new Vector2(0, -50.0f);
                shieldsOut = 0;
                playerNaenae = false;
            }
        }
    }

    private void shieldHit(string shieldD, RaycastHit2D rayCasted)
    {
        float shieldX = 0;

        // If your shield hit a wall when you shoot left...
        if(shieldD == "Left" && rayCasted.distance < shieldThrowDistance)
        {
            // Throws it into the wall at the distance for the cast given room for the full shield on the outside
            shieldX = this.transform.position.x - rayCasted.distance + (shieldSize / 2);
        }
        
        // again for the right
        else if(shieldD == "Right" && rayCasted.distance < shieldThrowDistance)
        {
            shieldX = this.transform.position.x + rayCasted.distance - (shieldSize / 2);
        }
        
        print("Found an object - distance: " + rayCasted.distance);

        // If you're ending up with a shield in your face...
        if(rayCasted.distance < 2.5f && grounded)
        {
            // Notify console
            print("You're in it, sicko");
            
        }

        // Spawns a shield at the shield direction
        spawnShield(shieldX, this.transform.position.y, shieldD);

        // Closeup audio + var changes
        audioSource.PlayOneShot(shieldthrow, 0.4F);
        switchCase = true;

        // Play throw animation
        animator = this.GetComponentInChildren<Animator>();
        animator.SetTrigger("throw");
    }

    private void shieldNoHit(string shieldD)
    {
        float shieldX = 0;

        // If your shield hit a wall when you shoot left...
        if(shieldD == "Left")
        {
            // Throws it into the wall at the distance for the cast given room for the full shield on the outside
            shieldX = this.transform.position.x - shieldThrowDistance + (shieldSize / 2);
        }
        
        // again for the right
        else if(shieldD == "Right")
        {
            shieldX = this.transform.position.x + shieldThrowDistance - (shieldSize / 2);
        }
        
        print("No object found :(");

        // Spawns a shield at the shield direction
        spawnShield(shieldX, this.transform.position.y, shieldD);
        shieldsThrownCount += 1;
        PlayerPrefs.SetInt("shieldsThrown", shieldsThrownCount);

        // Closeup audio + var changes
        audioSource.PlayOneShot(shieldthrow, 0.4F);
        switchCase = true;

        // Play throw animation
        animator = this.GetComponentInChildren<Animator>();
        animator.SetTrigger("throw");
    }

    IEnumerator shieldReset()
    {
        currentResetTime = 1f;

        if(Input.GetKeyUp(resetKey))
        {
            yield break;
        }

        while(true)
        {
            yield return new WaitForEndOfFrame();
            currentResetTime -= Time.deltaTime;
        
            if (currentResetTime <= 0)
            {
               foreach (GameObject shieldID in shieldsList)
               {
                   shieldID.transform.position = new Vector2(0, -50.0f);
               }

               shieldsOut = 0;
                audioSource.PlayOneShot(reload, 1.2F);
                yield break;
            }

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
            // Turn the shield on
            shieldsList[shieldsOut].SetActive(true);

            // Throw out the shield!
            shieldsList[shieldsOut].transform.position = new Vector3(shieldX, shieldY - .25f, 0);

            // Animate...
            animator = shieldsList[shieldsOut].GetComponentInChildren<Animator>();
            animator.SetTrigger(shieldD);
            
            // And update how many shields have been thrown!
            shieldsOut += 1;

            // Give the player a little "oomf" in the air.
            // Like cloud mario.
            if(!grounded)
            {
                shieldThrownInAir = true;
                rb.velocity = (new Vector2(rb.velocity.x, 0));
                rb.velocity += Vector2.up * odysseyJump;
                // Play squish animation
                animator = this.GetComponentInChildren<Animator>();
                animator.SetTrigger("jumpsquish");
            }
            
            
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

    private void addToList(string Tag, List<GameObject> listToAddTo)
    {
        foreach(GameObject shieldID in GameObject.FindGameObjectsWithTag(Tag)) 
        {
             listToAddTo.Add(shieldID);
        }
    }
}