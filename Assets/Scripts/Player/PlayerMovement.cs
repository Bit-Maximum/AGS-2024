using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    //Scriptable object which holds all the player's movement parameters. If you don't want to use it
    //just paste in all the parameters, though you will need to manuly change all references in this script

    //HOW TO: to add the scriptable object, right-click in the project window -> create -> Player Data
    //Next, drag it into the slot in playerMovement on your player

    public PlayerData Data;
    public PlayerStatus playerStatus;

    #region Variables
    //Components
    public Rigidbody2D RB { get; private set; }
    public Animator ANIM;
    private AudioManager audioManager;

    ////Equipment
    public bool IsEnteractive = false;
    public SpriteRenderer leftBoot;  //
    public SpriteRenderer rightBoot; //
    [SerializeField] public GameObject bootsLogic; // 

    private int spriteIterator;

    //Variables control the various actions the player can perform at any time.
    //These are fields which can are public allowing for other sctipts to read them
    //but can only be privately written to.
    public bool IsFacingRight { get; private set; }
    public bool IsJumping { get; private set; }
    public bool IsWallJumping { get; private set; }
    public bool IsSliding { get; private set; }

    //Timers (also all fields, could be private and a method returning a bool could be used)
    public float LastOnGroundTime { get; private set; }
    public float LastOnWallTime { get; private set; }
    public float LastOnWallRightTime { get; private set; }
    public float LastOnWallLeftTime { get; private set; }
    public float LastAccelerateTime { get; private set; }

    //Jump
    private bool _isJumpCut;
    private bool _isJumpFalling;
    private bool _canDoAnotherJump;

    //Wall Jump
    private float _wallJumpStartTime;
    private int _lastWallJumpDir;

    //Animator
    private bool IsJumpAnimStarted;
    private bool IsSprintAnimStarted;
    private bool IsSprinting;

    private Vector2 _moveInput;
    public float LastPressedJumpTime { get; private set; }

    //Attack
    public float AttackCooldownTime { get; private set; }
    private bool _isAttacking;

    //Set all of these up in the inspector
    [Header("Checks")]
    [SerializeField] private Transform _groundCheckPoint;
    //Size of groundCheck depends on the size of your character generally you want them slightly small than width (for ground) and height (for the wall check)
    [SerializeField] private Vector2 _groundCheckSize = new Vector2(0.49f, 0.03f);
    [Space(5)]
    [SerializeField] private Transform _frontWallCheckPoint;
    [SerializeField] private Transform _backWallCheckPoint;
    [SerializeField] private Vector2 _wallCheckSize = new Vector2(0.5f, 1f);
    [Space(5)]
    //Size of attack hitbox
    [SerializeField] private Transform _frontAttackCheckPoint;
    [SerializeField] private Vector2 _frontAttackCheckSize = new Vector2(0.5f, 1f);
    [SerializeField] private Transform _bottomAttackCheckPoint;
    [SerializeField] private Vector2 _bottomAttackCheckSize = new Vector2(0.5f, 1f);
    [Space(5)]
    [SerializeField] private Transform _interactiveCheckPoint;
    [SerializeField] private Vector2 _interactiveCheckSize = new Vector2(0.5f, 1f);

    [Header("Layers & Tags")]
    [SerializeField] private LayerMask _groundLayer;
    [SerializeField] private LayerMask _enemyLayer;
    [SerializeField] private LayerMask _playerLayer;
    [SerializeField] private LayerMask _platformLayer;
    [SerializeField] private LayerMask _InteractiveLayer;
    #endregion

    private void Awake()
    {
        RB = GetComponent<Rigidbody2D>();
        ANIM = GetComponent<Animator>();
        playerStatus = GetComponent<PlayerStatus>();
        audioManager = GameObject.FindGameObjectWithTag("Audio").GetComponent<AudioManager>();

        spriteIterator = 0;
    }

    private void Start()
    {
        leftBoot = GameObject.Find("LeftBoot").GetComponent<SpriteRenderer>();
        rightBoot = GameObject.Find("RightBoot").GetComponent<SpriteRenderer>();

        SetGravityScale(Data.gravityScale);
        IsFacingRight = true;
    }

    private void Update()
    {
        #region TIMERS
        LastOnGroundTime -= Time.deltaTime;
        LastOnWallTime -= Time.deltaTime;
        LastOnWallRightTime -= Time.deltaTime;
        LastOnWallLeftTime -= Time.deltaTime;

        LastPressedJumpTime -= Time.deltaTime;
        AttackCooldownTime -= Time.deltaTime;
        LastAccelerateTime -= Time.deltaTime;
        #endregion
    }

    private void FixedUpdate()
    {
        #region INPUT HANDLER
        if (_moveInput.x != 0)
            CheckDirectionToFace(_moveInput.x > 0);
        #endregion

        #region COLLISION CHECKS
        if (!IsJumping)
        {
            //Ground Check
            if (Physics2D.OverlapBox(_groundCheckPoint.position, _groundCheckSize, 0, _groundLayer) && !IsJumping) //checks if set box overlaps with ground
            {
                LastOnGroundTime = Data.coyoteTime; //if so sets the lastGrounded to coyoteTime
                //_canDoAnotherJump = false;
            }

            //Right Wall Check
            if (((Physics2D.OverlapBox(_frontWallCheckPoint.position, _wallCheckSize, 0, _groundLayer) && IsFacingRight)
                    || (Physics2D.OverlapBox(_backWallCheckPoint.position, _wallCheckSize, 0, _groundLayer) && !IsFacingRight)) && !IsWallJumping)
                LastOnWallRightTime = Data.coyoteTime;

            //Left Wall Check
            if (((Physics2D.OverlapBox(_frontWallCheckPoint.position, _wallCheckSize, 0, _groundLayer) && !IsFacingRight)
                || (Physics2D.OverlapBox(_backWallCheckPoint.position, _wallCheckSize, 0, _groundLayer) && IsFacingRight)) && !IsWallJumping)
                LastOnWallLeftTime = Data.coyoteTime;

            //Two checks needed for both left and right walls since whenever the play turns the wall checkPoints swap sides
            LastOnWallTime = Mathf.Max(LastOnWallLeftTime, LastOnWallRightTime);
        }
        #endregion

        #region ATTACKS CHECKS
        if (_isAttacking)
        {
            //Set attack cooldown
            AttackCooldownTime = Data.attackCooldownTime;
            _isAttacking = false;

            FrontAttack();
            // Choice attack type
            //if (_moveInput.y < 0){ // check if looking down
            //    // if so then perform bottomAttack
            //    BottomAttack();
            //}
            //else
            //{
            //    // otherwise perform default attack
            //    FrontAttack();
            //}
        }
        #endregion

        #region JUMP CHECKS
        if (IsJumping && RB.velocity.y < 0)
        {
            IsJumping = false;

            if (!IsWallJumping)
                _isJumpFalling = true;
        }

        if (IsWallJumping && Time.time - _wallJumpStartTime > Data.wallJumpTime)
        {
            IsWallJumping = false;
        }

        if (LastOnGroundTime > 0 && !IsJumping && !IsWallJumping)
        {
            _isJumpCut = false;

            if (!IsJumping)
                _isJumpFalling = false;
        }

        //Jump
        if (CanJump() && LastPressedJumpTime > 0)
        {
            IsJumping = true;
            IsWallJumping = false;
            _isJumpCut = false;
            _isJumpFalling = false;
            Jump();
        }
        //WALL JUMP
        else if (CanWallJump() && LastPressedJumpTime > 0)
        {
            IsWallJumping = true;
            IsJumping = false;
            _isJumpCut = false;
            _isJumpFalling = false;
            _wallJumpStartTime = Time.time;
            _lastWallJumpDir = (LastOnWallRightTime > 0) ? -1 : 1;

            WallJump(_lastWallJumpDir);
        }
        #endregion

        #region SLIDE CHECKS
        if (CanSlide() && ((LastOnWallLeftTime > 0 && _moveInput.x < 0) || (LastOnWallRightTime > 0 && _moveInput.x > 0)))
            IsSliding = true;
        else
            IsSliding = false;
        #endregion

        #region GRAVITY
        //Higher gravity if we've released the jump input or are falling
        if (IsSliding)
        {
            SetGravityScale(0);
        }
        else if (RB.velocity.y < 0 && _moveInput.y < 0)
        {
            //Much higher gravity if holding down
            SetGravityScale(Data.gravityScale * Data.fastFallGravityMult);
            //Caps maximum fall speed, so when falling over large distances we don't accelerate to insanely high speeds
            RB.velocity = new Vector2(RB.velocity.x, Mathf.Max(RB.velocity.y, -Data.maxFastFallSpeed));
        }
        else if (_isJumpCut)
        {
            //Higher gravity if jump button released
            SetGravityScale(Data.gravityScale * Data.jumpCutGravityMult);
            RB.velocity = new Vector2(RB.velocity.x, Mathf.Max(RB.velocity.y, -Data.maxFallSpeed));
        }
        else if ((IsJumping || IsWallJumping || _isJumpFalling) && Mathf.Abs(RB.velocity.y) < Data.jumpHangTimeThreshold)
        {
            SetGravityScale(Data.gravityScale * Data.jumpHangGravityMult);
        }
        else if (RB.velocity.y < 0)
        {
            //Higher gravity if falling
            SetGravityScale(Data.gravityScale * Data.fallGravityMult);
            //Caps maximum fall speed, so when falling over large distances we don't accelerate to insanely high speeds
            RB.velocity = new Vector2(RB.velocity.x, Mathf.Max(RB.velocity.y, -Data.maxFallSpeed));
        }
        else
        {
            //Default gravity if standing on a platform or moving upwards
            SetGravityScale(Data.gravityScale);
        }
        #endregion

        #region ANIMATION HANDLER
        if (IsJumping || IsWallJumping || _isJumpFalling)
        {
            if (!IsJumpAnimStarted)
                PlayJumpAnim();
        }
        else
        {
            StopJumpAnim();
        }
        #endregion

        #region RUN HANDLER
        if ((IsSprinting || IsSprintAnimStarted) && (RB.velocity.x == 0 && Mathf.Abs(_moveInput.x) < 0.2f))
        {
            ANIM.SetBool("Sprinting", false);
            IsSprintAnimStarted = false;
            IsSprinting = false;
        }

        if (IsSprintAnimStarted && LastAccelerateTime < 0)
        {
            IsSprinting = true;
            IsSprintAnimStarted = false;
            ANIM.SetBool("Sprinting", true);
            audioManager.PlaySFX(audioManager.Run);
        }

        //Handle Run
        if (IsWallJumping)
            Run(Data.wallJumpRunLerp);
        else
            Run(1);

        //Handle Slide
        if (IsSliding)
            Slide();
        #endregion
    }

    #region INPUT SYSTEM
    private void OnMove(InputValue value)
    {
        _moveInput.x = value.Get<Vector2>().x;
        if (!IsJumping && !IsWallJumping)
        {
            ANIM.SetFloat("Run", Mathf.Abs(_moveInput.x));
            if (!IsSprintAnimStarted && LastAccelerateTime < 0)
            {
                LastAccelerateTime = 3f;
                IsSprintAnimStarted = true;
            }
        }

        
    }

    private void OnExit(InputValue value)
    {
        Debug.Log("Exit");
        playerStatus.Die();
    }

    private void OnLook(InputValue value)
    {
        _moveInput.y = value.Get<Vector2>().y;
        if (_moveInput.y < 0)
        {
            Collider2D platformColited = Physics2D.OverlapBox(_groundCheckPoint.position, _groundCheckSize, 0, _platformLayer);
            if (platformColited)
            {
                StartCoroutine(TemporalyDisablePlatformCollision(platformColited));
            }
        }

    }

    private void OnJumpPress(InputValue value)
    {
        Collider2D item = Physics2D.OverlapBox(_bottomAttackCheckPoint.position, _bottomAttackCheckSize, 0, _InteractiveLayer);
        if (item) //checks if set box overlaps with any Enemy
        {
            Flashlight flashlight = item.GetComponent<Flashlight>();
            if (flashlight.IsActive)
            {
                flashlight.SetDisabled();
                playerStatus.ChangeScoreMultyplier(0);

                _canDoAnotherJump = true; //if so we can do another jump in the air
            }
        }
        LastPressedJumpTime = Data.jumpInputBufferTime;
    }

    private void OnJumpRelease(InputValue value)
    {
        
        if (CanJumpCut() || CanWallJumpCut())
            _isJumpCut = true;
    }

    private void OnAttack(InputValue value)
    {
        if (CanAttack())
            _isAttacking = true;
    }

    private void OnChangeSprite(InputValue value)
    {
        if (spriteIterator > Data.spriteArray.Length - 1)
            spriteIterator = 0;

        if (leftBoot)
            leftBoot.sprite = Data.spriteArray[spriteIterator];

        if (rightBoot)
            rightBoot.sprite = Data.spriteArray[spriteIterator];
        spriteIterator++;
    }

    private void OnEnteractPress(InputValue value)
    {
        IsEnteractive = true;
    }

    private void OnEnteractRelease(InputValue value)
    {
        IsEnteractive = false;
    }
    #endregion

    #region GENERAL METHODS
    public void SetGravityScale(float scale)
    {
        RB.gravityScale = scale;
    }
    #endregion

    //MOVEMENT METHODS
    #region RUN METHODS
    private void Run(float lerpAmount)
    {
        //Calculate the direction we want to move in and our desired velocity
        float targetSpeed = _moveInput.x * Data.runMaxSpeed;
        //We can reduce are control using Lerp() this smooths changes to are direction and speed
        targetSpeed = Mathf.Lerp(RB.velocity.x, targetSpeed, lerpAmount);

        #region Calculate AccelRate
        float accelRate;

        //Gets an acceleration value based on if we are accelerating (includes turning) 
        //or trying to decelerate (stop). As well as applying a multiplier if we're air borne.
        if (LastOnGroundTime > 0)
            accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? Data.runAccelAmount : Data.runDeccelAmount;
        else
            accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? Data.runAccelAmount * Data.accelInAir : Data.runDeccelAmount * Data.deccelInAir;
        #endregion

        #region Add Bonus Jump Apex Acceleration
        //Increase are acceleration and maxSpeed when at the apex of their jump, makes the jump feel a bit more bouncy, responsive and natural
        if ((IsJumping || IsWallJumping || _isJumpFalling) && Mathf.Abs(RB.velocity.y) < Data.jumpHangTimeThreshold)
        {
            accelRate *= Data.jumpHangAccelerationMult;
            targetSpeed *= Data.jumpHangMaxSpeedMult;
        }
        #endregion

        #region Conserve Momentum
        //We won't slow the player down if they are moving in their desired direction but at a greater speed than their maxSpeed
        if (Data.doConserveMomentum && Mathf.Abs(RB.velocity.x) > Mathf.Abs(targetSpeed) && Mathf.Sign(RB.velocity.x) == Mathf.Sign(targetSpeed) && Mathf.Abs(targetSpeed) > 0.01f && LastOnGroundTime < 0)
        {
            //Prevent any deceleration from happening, or in other words conserve are current momentum
            //You could experiment with allowing for the player to slightly increae their speed whilst in this "state"
            accelRate = 0;
        }
        #endregion

        //Calculate difference between current velocity and desired velocity
        float speedDif = targetSpeed - RB.velocity.x;
        //Calculate force along x-axis to apply to the player

        float movement = speedDif * accelRate;
        //Convert this to a vector and apply to rigidbody
        RB.AddForce(movement * Vector2.right, ForceMode2D.Force);
        if (RB.velocity.x != 0)
        {
            if (IsSprinting)
            {
                audioManager.PlayColapsSFX(audioManager.Run);
            }
            else
            {
                audioManager.PlayColapsSFX(audioManager.Walk);
            }
        }
        /*
		 * For those interested here is what AddForce() will do
		 * RB.velocity = new Vector2(RB.velocity.x + (Time.fixedDeltaTime  * speedDif * accelRate) / RB.mass, RB.velocity.y);
		 * Time.fixedDeltaTime is by default in Unity 0.02 seconds equal to 50 FixedUpdate() calls per second
		*/
    }

    private void Turn()
    {
        //stores scale and flips the player along the x axis, 
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;

        IsFacingRight = !IsFacingRight;
    }
    #endregion

    #region JUMP METHODS
    private void Jump()
    {
        //Ensures we can't call Jump multiple times from one press
        LastPressedJumpTime = 0;
        LastOnGroundTime = 0;
        _canDoAnotherJump = false;

        audioManager.PlaySFX(audioManager.Jump);
        #region Perform Jump
        //We increase the force applied if we are falling
        //This means we'll always feel like we jump the same amount 
        //(setting the player's Y velocity to 0 beforehand will likely work the same, but I find this more elegant :D)
        float force = Data.jumpForce;
        if (RB.velocity.y < 0)
            force -= RB.velocity.y;

        RB.AddForce(Vector2.up * force, ForceMode2D.Impulse);
        #endregion
    }

    private void WallJump(int dir)
    {
        //Ensures we can't call Wall Jump multiple times from one press
        LastPressedJumpTime = 0;
        LastOnGroundTime = 0;
        LastOnWallRightTime = 0;
        LastOnWallLeftTime = 0;

        audioManager.PlaySFX(audioManager.Jump);
        #region Perform Wall Jump
        Vector2 force = new Vector2(Data.wallJumpForce.x, Data.wallJumpForce.y);
        force.x *= dir; //apply force in opposite direction of wall

        if (Mathf.Sign(RB.velocity.x) != Mathf.Sign(force.x))
            force.x -= RB.velocity.x;

        if (RB.velocity.y < 0) //checks whether player is falling, if so we subtract the velocity.y (counteracting force of gravity). This ensures the player always reaches our desired jump force or greater
            force.y -= RB.velocity.y;

        //Unlike in the run we want to use the Impulse mode.
        //The default mode will apply are force instantly ignoring masss
        RB.AddForce(force, ForceMode2D.Impulse);
        #endregion
    }
    #endregion

    #region ATTACKS METHODS
    private void FrontAttack()
    {
        audioManager.PlaySFX(audioManager.FrontAttack);
        ANIM.SetTrigger("AttackFront");
        //Front Attack Check
        Collider2D enemy = Physics2D.OverlapBox(_frontAttackCheckPoint.position, _frontAttackCheckSize, 0, _enemyLayer);
        if (enemy) //checks if set box overlaps with any Enemy
        {
            EnemyStatus enemyStatus = enemy.GetComponent<EnemyStatus>();
            playerStatus.ChangeScoreMultyplier(1);
            playerStatus.ChageScore(enemyStatus.GetScore());
            enemyStatus.TakeDamage(Data.attackDamage);

            _canDoAnotherJump = true; //if so we can do another jump in the air
            HorisontalAttackFeedback();
        }

        Collider2D item = Physics2D.OverlapBox(_frontAttackCheckPoint.position, _frontAttackCheckSize, 0, _InteractiveLayer);
        if (item) //checks if set box overlaps with any Enemy
        {
            Flashlight flashlight = item.GetComponent<Flashlight>();
            if (flashlight.IsActive)
            {
                flashlight.SetDisabled();
                playerStatus.ChangeScoreMultyplier(0);

                _canDoAnotherJump = true; //if so we can do another jump in the air
            }
        }
    }

    private void BottomAttack()
    {
        audioManager.PlaySFX(audioManager.FrontAttack);
        ANIM.SetTrigger("AttackDown");
        //Down Attack Check
        Collider2D enemy = Physics2D.OverlapBox(_bottomAttackCheckPoint.position, _bottomAttackCheckSize, 0, _enemyLayer);
        if (enemy) //checks if set box overlaps with any Enemy
        {
            EnemyStatus enemyStatus = enemy.GetComponent<EnemyStatus>();
            playerStatus.ChangeScoreMultyplier(1);
            playerStatus.ChageScore(enemyStatus.GetScore());
            enemyStatus.TakeDamage(Data.attackDamage);

            _canDoAnotherJump = true; //if so we can do another jump in the air
            VerticalAttackFeedback();
        }
    }
    #endregion

    #region ATTACK FEEDBACK METHODS
    // Add brief impuls in oposite direction to make attacks feels more natural 
    private void VerticalAttackFeedback()
    {
        if (!Data.doVerticalAttackFeedback)
            return;

        //We increase the force applied if we are falling
        //This means we'll always push at the same height
        float force = Data.verticalAttackFeedbackForce;
        if (RB.velocity.y < 0)
            force -= RB.velocity.y;

        RB.AddForce(Vector2.up * force, ForceMode2D.Impulse);
    }

    private void HorisontalAttackFeedback()
    {
        if (!Data.doHorisontalAttackFeedback)
            return;

        //We increase the force applied if we are running it to target
        //This means we'll push player away from target or atleast stop player from runing into enemy
        float force = Data.verticalAttackFeedbackForce;
        if (Mathf.Abs(RB.velocity.x) > 0)
            force += Mathf.Abs(RB.velocity.x);

        RB.AddForce(Vector2.left * force * Mathf.Sign(RB.velocity.x), ForceMode2D.Impulse);
    }
    #endregion

    #region OTHER MOVEMENT METHODS
    private void Slide()
    {
        //Works the same as the Run but only in the y-axis
        //THis seems to work fine, buit maybe you'll find a better way to implement a slide into this system
        float speedDif = Data.slideSpeed - RB.velocity.y;
        float movement = speedDif * Data.slideAccel;
        //So, we clamp the movement here to prevent any over corrections (these aren't noticeable in the Run)
        //The force applied can't be greater than the (negative) speedDifference * by how many times a second FixedUpdate() is called. For more info research how force are applied to rigidbodies.
        movement = Mathf.Clamp(movement, -Mathf.Abs(speedDif) * (1 / Time.fixedDeltaTime), Mathf.Abs(speedDif) * (1 / Time.fixedDeltaTime));

        RB.AddForce(movement * Vector2.up);
    }

    private IEnumerator TemporalyDisablePlatformCollision(Collider2D platformCollider)
    {
        if (platformCollider)
        {
            Collider2D playerCollider = GetComponent<BoxCollider2D>();
            Physics2D.IgnoreCollision(playerCollider, platformCollider, true);
            yield return new WaitForSeconds(0.6f);
            Physics2D.IgnoreCollision(playerCollider, platformCollider, false);
        }
    }
    #endregion

    #region CHECK METHODS
    public void CheckDirectionToFace(bool isMovingRight)
    {
        if (isMovingRight != IsFacingRight)
            Turn();
    }

    private bool CanJump()
    {
        

        return (LastOnGroundTime > 0 && !IsJumping) || _canDoAnotherJump;
    }

    private bool CanWallJump()
    {
          return (LastPressedJumpTime > 0 && LastOnWallTime > 0 && LastOnGroundTime <= 0 && !IsWallJumping ||
             (LastOnWallRightTime > 0 && _lastWallJumpDir == 1) || (LastOnWallLeftTime > 0 && _lastWallJumpDir == -1));
    }

    private bool CanJumpCut()
    {
        return IsJumping && RB.velocity.y > 0;
    }

    private bool CanWallJumpCut()
    {
        return IsWallJumping && RB.velocity.y > 0;
    }

    public bool CanSlide()
    {
        if (LastOnWallTime > 0 && !IsJumping && !IsWallJumping && LastOnGroundTime <= 0)
            return true;
        else
            return false;
    }

    private bool CanAttack()
    {
        return AttackCooldownTime < 0;
    }
    #endregion

    #region ANIMATOR FUNCTIONS
    public void PlayJumpAnim()
    {
        IsJumpAnimStarted = false;
        ANIM.SetBool("Jump", true);
    }

    public void StopJumpAnim()
    {
        IsJumpAnimStarted = false;
        ANIM.SetBool("Jump", false);
    }
    #endregion

    #region EDITOR METHODS
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(_groundCheckPoint.position, _groundCheckSize);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(_frontWallCheckPoint.position, _wallCheckSize);
        Gizmos.DrawWireCube(_backWallCheckPoint.position, _wallCheckSize);
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(_bottomAttackCheckPoint.position, _bottomAttackCheckSize);
        Gizmos.DrawWireCube(_frontAttackCheckPoint.position, _frontAttackCheckSize);
    }
    #endregion
}
