using System;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
	public bool jumping { get; set; }

	public bool sliding { get; set; }

	public bool crouching { get; set; }

    public bool flyDown { get; set; }

    public bool sprinting { get; private set; }

    public bool flying { get; private set; }

    public bool noclip { get; set; }

    public static PlayerMovement Instance { get; private set; }

    private void Awake()
    {
        PlayerMovement.Instance = this;
        this.rb = base.GetComponent<Rigidbody>();
        this.playerStatus = base.GetComponent<PlayerStatus>();
    }

    private void Start()
    {
        this.playerScale = base.transform.localScale;
        this.playerCollider = base.GetComponent<Collider>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        if (this.dead)
        {
            return;
        }
        this.FootSteps();
        this.fallSpeed = this.rb.velocity.y;
    }

	public Vector2 GetInput()
	{
		return new Vector2(this.x, this.y);
	}

    public void SetInput(Vector2 dir, bool crouching, bool jumping, bool sprinting)
    {
        this.x = dir.x;
        this.y = dir.y;
        this.crouching = crouching && !CurrentSettings.Instance.disableCrouch;
        this.flyDown = crouching;
        this.jumping = jumping;
        this.sprinting = sprinting;
    }

    private void CheckInput()
    {
        if (this.crouching && !this.sliding)
        {
            this.StartCrouch();
        }
        if (!this.crouching && this.sliding)
        {
            this.StopCrouch();
        }
        if (this.flying)
        {
            this.maxSpeed = float.PositiveInfinity;
            return;
        }
        if (this.sprinting && this.playerStatus.CanRun())
        {
            this.maxSpeed = this.maxRunSpeed;
            return;
        }
        this.maxSpeed = this.maxWalkSpeed;
    }

    public void StartCrouch()
    {
        if (GameManager.gameSettings.gameMode == GameSettings.GameMode.Creative) return;
        if (this.sliding)
        {
            return;
        }
        this.sliding = true;
        base.transform.localScale = this.crouchScale;
        base.transform.position = new Vector3(base.transform.position.x, base.transform.position.y - 0.65f, base.transform.position.z);
        if (this.rb.velocity.magnitude > 0.5f && this.grounded)
        {
            this.rb.AddForce(this.orientation.transform.forward * this.slideForce);
        }
    }

    public void StopCrouch()
    {
        this.sliding = false;
        base.transform.localScale = this.playerScale;
        base.transform.position = new Vector3(base.transform.position.x, base.transform.position.y + 0.65f, base.transform.position.z);
    }

    private void FootSteps()
    {
        if (this.crouching || this.dead)
        {
            return;
        }
        if (this.grounded)
        {
            float num = 1f;
            float num2 = this.rb.velocity.magnitude;
            if (num2 > 20f)
            {
                num2 = 20f;
            }
            this.distance += num2 * Time.deltaTime * 50f;
            if (this.distance > 300f / num)
            {
                Instantiate<GameObject>(this.footstepFx, base.transform.position, Quaternion.identity);
                this.distance = 0f;
            }
        }
    }

	private void WaterMovement()
	{
		float num = 1f;
		if (this.jumping)
		{
			num *= 2f;
		}
		this.rb.AddForce(Vector3.up * this.rb.mass * -Physics.gravity.y * num);
		float d = 1f;
		if (PlayerStatus.Instance.stamina <= 0f)
		{
			d = 0.5f;
		}
		this.rb.AddForce(this.playerCam.transform.forward * this.y * this.swimSpeed * d);
		this.rb.AddForce(this.orientation.transform.right * this.x * this.swimSpeed * d);
	}

	public bool IsUnderWater()
	{
		float num = World.Instance.water.position.y;
		return base.transform.position.y < num;
	}

    public void Movement(float x, float y)
    {
        UpdateCollisionChecks();
        this.x = x;
        this.y = y;
        if (dead)
        {
            return;
        }
        CheckInput();
        if (WorldUtility.WorldHeightToBiome(base.transform.position.y + 3.2f) == TextureData.TerrainType.Water)
        {
            maxSpeed *= 0.4f;
        }
        if (IsUnderWater())
        {
            if (rb.drag <= 0f)
            {
                rb.drag = 1f;
            }
            WaterMovement();
            return;
        }
        if (rb.drag > 0f)
        {
            rb.drag = 0f;
        }
        if (!grounded)
        {
            rb.AddForce(Vector3.down * extraGravity);
        }
        Vector2 mag = FindVelRelativeToLook();
        float num = mag.x;
        float num2 = mag.y;
        CounterMovement(x, y, mag);
        RampMovement(mag);
        if (readyToJump && jumping && grounded)
        {
            Jump();
        }
        if (crouching && grounded && readyToJump)
        {
            rb.AddForce(Vector3.down * 60f);
            return;
        }
        float num3 = x;
        float num4 = y;
        float num5 = maxSpeed * PowerupInventory.Instance.GetSpeedMultiplier(null);
        if (x > 0f && num > num5)
        {
            num3 = 0f;
        }
        if (x < 0f && num < 0f - num5)
        {
            num3 = 0f;
        }
        if (y > 0f && num2 > num5)
        {
            num4 = 0f;
        }
        if (y < 0f && num2 < 0f - num5)
        {
            num4 = 0f;
        }
        float num6 = 1f;
        float num7 = 1f;
        if (!grounded)
        {
            num6 = 0.2f;
            num7 = 0.2f;
            if (IsHoldingAgainstVerticalVel(mag))
            {
                float num8 = Mathf.Abs(mag.y * 0.025f);
                if (num8 < 0.5f)
                {
                    num8 = 0.5f;
                }
                num7 = Mathf.Abs(num8);
            }
        }
        if (grounded && crouching)
        {
            num7 = 0f;
        }
        if (surfing)
        {
            num6 = 0.6f;
            num7 = 0.3f;
        }
        float num9 = 0.01f;
        rb.AddForce(orientation.forward * num4 * moveSpeed * 0.02f * num7);
        rb.AddForce(orientation.right * num3 * moveSpeed * 0.02f * num6);
        if (!grounded)
        {
            if (num3 != 0f)
            {
                rb.AddForce(-orientation.forward * mag.y * moveSpeed * 0.02f * num9);
            }
            if (num4 != 0f)
            {
                rb.AddForce(-orientation.right * mag.x * moveSpeed * 0.02f * num9);
            }
        }
        if (!readyToJump)
        {
            resetJumpCounter++;
            if (resetJumpCounter >= jumpCounterResetTime)
            {
                ResetJump();
            }
        }
    }
	
	public void PushPlayer()
	{
		this.pushed = true;
		Invoke(nameof(ResetPush), 0.3f);
	}

	private void ResetPush()
	{
		this.pushed = false;
	}

    private void RampMovement(Vector2 mag)
    {
        if (this.grounded && this.onRamp && !this.surfing && !this.crouching && !this.jumping && this.resetJumpCounter >= this.jumpCounterResetTime && Math.Abs(this.x) < 0.05f && Math.Abs(this.y) < 0.05f && !this.pushed)
        {
            this.rb.useGravity = false;
            if (this.rb.velocity.y > 0f)
            {
                this.rb.velocity = new Vector3(this.rb.velocity.x, 0f, this.rb.velocity.z);
                return;
            }
            if (this.rb.velocity.y <= 0f && Math.Abs(mag.magnitude) < 1f)
            {
                this.rb.velocity = Vector3.zero;
                return;
            }
        }
        else if (!flying)
        {
            this.rb.useGravity = true;
        }
    }

    private void ResetJump()
    {
        this.readyToJump = true;
        base.CancelInvoke(nameof(JumpCooldown));
    }

    DateTime lastJump = DateTime.MinValue;

    public void Jump()
    {
        if ((grounded || surfing || (!grounded && jumps > 0)) && readyToJump && playerStatus.CanJump())
        {
            if (grounded)
            {
                jumps = PowerupInventory.Instance.GetExtraJumps();
            }
            rb.isKinematic = false;
            if (!grounded)
            {
                jumps--;
            }
            readyToJump = false;
            CancelInvoke(nameof(JumpCooldown));
            Invoke(nameof(JumpCooldown), 0.25f);
            resetJumpCounter = 0;
            float num = jumpForce * PowerupInventory.Instance.GetJumpMultiplier();
            rb.AddForce(Vector3.up * num * 1.5f, ForceMode.Impulse);
            rb.AddForce(normalVector * num * 0.5f, ForceMode.Impulse);
            Vector3 velocity = rb.velocity;
            if (rb.velocity.y < 0.5f)
            {
                rb.velocity = new Vector3(velocity.x, 0f, velocity.z);
            }
            else if (rb.velocity.y > 0f)
            {
                rb.velocity = new Vector3(velocity.x, 0f, velocity.z);
            }
            ParticleSystem.VelocityOverLifetimeModule velocityOverLifetime = UnityEngine.Object.Instantiate(playerJumpSmokeFx, base.transform.position, Quaternion.LookRotation(Vector3.up)).GetComponent<ParticleSystem>().velocityOverLifetime;
            velocityOverLifetime.x = rb.velocity.x * 2f;
            velocityOverLifetime.z = rb.velocity.z * 2f;
            playerStatus.Jump();
        }
    }
    private void JumpCooldown()
    {
        this.readyToJump = true;
    }

    private void CounterMovement(float x, float y, Vector2 mag)
    {
        if (x == 0f && y == 0f && this.rb.velocity.magnitude < 1f && this.grounded && !this.jumping && this.playerStatus.CanJump())
        {
            this.rb.isKinematic = true;
        }
        else
        {
            this.rb.isKinematic = false;
        }
        if (!this.grounded || (this.jumping && this.playerStatus.CanJump()))
        {
            return;
        }
        if (this.crouching)
        {
            this.rb.AddForce(this.moveSpeed * 0.02f * -this.rb.velocity.normalized * this.slideCounterMovement);
            return;
        }
        if (Math.Abs(mag.x) > this.threshold && Math.Abs(x) < 0.05f && this.readyToCounterX > 1)
        {
            this.rb.AddForce(this.moveSpeed * this.orientation.transform.right * 0.02f * -mag.x * this.counterMovement);
        }
        if (Math.Abs(mag.y) > this.threshold && Math.Abs(y) < 0.05f && this.readyToCounterY > 1)
        {
            this.rb.AddForce(this.moveSpeed * this.orientation.transform.forward * 0.02f * -mag.y * this.counterMovement);
        }
        if (this.IsHoldingAgainstHorizontalVel(mag))
        {
            this.rb.AddForce(this.moveSpeed * this.orientation.transform.right * 0.02f * -mag.x * this.counterMovement * 2f);
        }
        if (this.IsHoldingAgainstVerticalVel(mag))
        {
            this.rb.AddForce(this.moveSpeed * this.orientation.transform.forward * 0.02f * -mag.y * this.counterMovement * 2f);
        }
        if (Mathf.Sqrt(Mathf.Pow(this.rb.velocity.x, 2f) + Mathf.Pow(this.rb.velocity.z, 2f)) > this.maxSpeed * PowerupInventory.Instance.GetSpeedMultiplier(null))
        {
            float num = this.rb.velocity.y;
            Vector3 vector = this.rb.velocity.normalized * this.maxSpeed * PowerupInventory.Instance.GetSpeedMultiplier(null);
            this.rb.velocity = new Vector3(vector.x, num, vector.z);
        }
        if (Math.Abs(x) < 0.05f)
        {
            this.readyToCounterX++;
        }
        else
        {
            this.readyToCounterX = 0;
        }
        if (Math.Abs(y) < 0.05f)
        {
            this.readyToCounterY++;
            return;
        }
        this.readyToCounterY = 0;
    }

    private bool IsHoldingAgainstHorizontalVel(Vector2 vel)
    {
        return (vel.x < -this.threshold && this.x > 0f) || (vel.x > this.threshold && this.x < 0f);
    }

    private bool IsHoldingAgainstVerticalVel(Vector2 vel)
    {
        return (vel.y < -this.threshold && this.y > 0f) || (vel.y > this.threshold && this.y < 0f);
    }

    public Vector2 FindVelRelativeToLook()
    {
        float current = this.orientation.transform.eulerAngles.y;
        float target = Mathf.Atan2(this.rb.velocity.x, this.rb.velocity.z) * 57.29578f;
        float num = Mathf.DeltaAngle(current, target);
        float num2 = 90f - num;
        float magnitude = new Vector2(this.rb.velocity.x, this.rb.velocity.z).magnitude;
        float num3 = magnitude * Mathf.Cos(num * 0.017453292f);
        return new Vector2(magnitude * Mathf.Cos(num2 * 0.017453292f), num3);
    }

    private bool IsFloor(Vector3 v)
    {
        return Vector3.Angle(Vector3.up, v) < this.maxSlopeAngle;
    }

    private bool IsSurf(Vector3 v)
    {
        float num = Vector3.Angle(Vector3.up, v);
        return num < 89f && num > this.maxSlopeAngle;
    }

    private bool IsWall(Vector3 v)
    {
        return Math.Abs(90f - Vector3.Angle(Vector3.up, v)) < 0.1f;
    }

    private bool IsRoof(Vector3 v)
    {
        return v.y == -1f;
    }

    private void OnCollisionEnter(Collision other)
    {
        int layer = other.gameObject.layer;
        Vector3 normal = other.contacts[0].normal;
        if (this.whatIsGround != (this.whatIsGround | 1 << layer))
        {
            return;
        }
        if (this.IsFloor(normal) && this.fallSpeed < -12f)
        {
            MoveCamera.Instance.BobOnce(new Vector3(0f, this.fallSpeed, 0f));
            Vector3 point = other.contacts[0].point;
            ParticleSystem.VelocityOverLifetimeModule velocityOverLifetime = Instantiate<GameObject>(this.playerSmokeFx, point, Quaternion.LookRotation(base.transform.position - point)).GetComponent<ParticleSystem>().velocityOverLifetime;
            velocityOverLifetime.x = this.rb.velocity.x * 2f;
            velocityOverLifetime.z = this.rb.velocity.z * 2f;
        }
    }

    private void OnCollisionStay(Collision other)
    {
        int layer = other.gameObject.layer;
        if (this.whatIsGround != (this.whatIsGround | 1 << layer))
        {
            return;
        }
        for (int i = 0; i < other.contactCount; i++)
        {
            Vector3 normal = other.contacts[i].normal;
			normal = new Vector3(normal.x, Mathf.Abs(normal.y), normal.z);
            if (this.IsFloor(normal))
            {
                if (!this.grounded)
                {
                    bool crouching = this.crouching;
                }
                if (Vector3.Angle(Vector3.up, normal) > 1f)
                {
                    this.onRamp = true;
                }
                else
                {
                    this.onRamp = false;
                }
                this.grounded = true;
                this.normalVector = normal;
                this.cancellingGrounded = false;
                this.groundCancel = 0;
            }
            if (this.IsSurf(normal))
            {
                this.surfing = true;
                this.cancellingSurf = false;
                this.surfCancel = 0;
            }
        }
    }

    private void UpdateCollisionChecks()
    {
        if (!this.cancellingGrounded)
        {
            this.cancellingGrounded = true;
        }
        else
        {
            this.groundCancel++;
            if ((float)this.groundCancel > this.delay)
            {
                this.StopGrounded();
            }
        }
        if (!this.cancellingSurf)
        {
            this.cancellingSurf = true;
            this.surfCancel = 1;
            return;
        }
        this.surfCancel++;
        if ((float)this.surfCancel > this.delay)
        {
            this.StopSurf();
        }
    }

    private void StopGrounded()
    {
        this.grounded = false;
    }

    private void StopSurf()
    {
        this.surfing = false;
    }

    public Vector3 GetVelocity()
    {
        return this.rb.velocity;
    }

    public float GetFallSpeed()
    {
        return this.rb.velocity.y;
    }

    public Collider GetPlayerCollider()
    {
        return this.playerCollider;
    }

    public Transform GetPlayerCamTransform()
    {
        return this.playerCam.transform;
    }

    public Vector3 HitPoint()
    {
        RaycastHit[] array = Physics.RaycastAll(this.playerCam.transform.position, this.playerCam.transform.forward, 100f, this.whatIsHittable);
        if (array.Length < 1)
        {
            return this.playerCam.transform.position + this.playerCam.transform.forward * 100f;
        }
        if (array.Length > 1)
        {
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i].transform.gameObject.layer == LayerMask.NameToLayer("Enemy") || array[i].transform.gameObject.layer == LayerMask.NameToLayer("Object") || array[i].transform.gameObject.layer == LayerMask.NameToLayer("Ground"))
                {
                    return array[i].point;
                }
            }
        }
        return array[0].point;
    }

    public bool IsCrouching()
    {
        return this.crouching;
    }

    public bool IsDead()
    {
        return this.dead;
    }

    public Rigidbody GetRb()
    {
        return this.rb;
    }

    public GameObject playerJumpSmokeFx;

    public GameObject footstepFx;

    public Transform playerCam;

    public Transform orientation;

    private Rigidbody rb;

    public bool dead;

    private float moveSpeed = 3500f;

    private float maxWalkSpeed = 6.5f;

    private float maxRunSpeed = 13f;

    private float maxSpeed = 6.5f;

    public bool grounded;

    public LayerMask whatIsGround;

    public float extraGravity = 5f;

    private Vector3 crouchScale = new Vector3(1f, 1.05f, 1f);

    private Vector3 playerScale;

    private float slideForce = 800f;

    private float slideCounterMovement = 0.12f;

    private bool readyToJump = true;

    private float jumpCooldown = 0.25f;

    private float jumpForce = 12f;

    private int jumps = 1;

    private float x;

    private float y;

    private float mouseDeltaX;

    private float mouseDeltaY;

    private Vector3 normalVector;

    public ParticleSystem ps;

    private ParticleSystem.EmissionModule psEmission;

    private Collider playerCollider;

    private float fallSpeed;

    public GameObject playerSmokeFx;

    private PlayerStatus playerStatus;

    private float distance;

	private float swimSpeed = 50f;

	private bool pushed;

	private bool onRamp;

    private int extraJumps;

    private int resetJumpCounter;

    private int jumpCounterResetTime = 10;

    private float counterMovement = 0.14f;

    private float threshold = 0.01f;

    private int readyToCounterX;

    private int readyToCounterY;

    private bool cancelling;

    private float maxSlopeAngle = 50f;

    private bool airborne;

    private bool onGround;

    private bool surfing;

    private bool cancellingGrounded;

    private bool cancellingSurf;

    private float delay = 5f;

    private int groundCancel;

    private int wallCancel;

    private int surfCancel;

    public LayerMask whatIsHittable;

    private float vel;
}
