using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Random = System.Random;


public struct FrameInput
{
	public bool JumpDown;
	public bool JumpHeld;
	public Vector2 Move;
};

public interface IPlayerController
{
	public event Action<bool, float> GroundedChanged;

	public event Action Jumped;
	public Vector2 FrameInput { get; }
}

public class PlayerController : MonoBehaviour, IPlayerController
{
	[SerializeField] private ScriptableStats _stats;
	
	// main game
	private Rigidbody2D _rb;
	private CapsuleCollider2D _col;
	private FrameInput _input;
	private Vector2 _FrameVelocity;
	private KeyCode rightKey = KeyCode.D;
	private KeyCode leftKey = KeyCode.A;
	private KeyCode jumpKey = KeyCode.Space;
	private bool _cachedQueryStartInColliders;
	private float _time;
	
	// interfaces
	
	public Vector2 FrameInput => _input.Move;
	public event Action<bool, float> GroundedChanged;
	public event Action Jumped;

	// keybinds
	private int i;
	private Dictionary<int, KeyCode> dict = new Dictionary<int, KeyCode>();
	private KeyCode ChangeSide = KeyCode.Tab;
	public Transform _gm;
	
	void Awake()
	{
		_rb = GetComponent<Rigidbody2D>();
		_col = GetComponent<CapsuleCollider2D>();
		
		_cachedQueryStartInColliders = Physics2D.queriesStartInColliders;
		Spawn();
	}
    void Start()
    {
	    i = 0;
	    _time += Time.deltaTime;
	    for (KeyCode k = KeyCode.A; k <= KeyCode.Z; k++)
	    {
		    dict.Add(i++, k);
	    }
	    
	    for (KeyCode k = KeyCode.Alpha0; k <= KeyCode.Alpha9; k++)
	    {
		    dict.Add(i++, k);
	    }
	    
	    dict.Add(i++, KeyCode.Minus);
	    dict.Add(i++, KeyCode.Plus);


	    
    }

    void ChangeKeyBind(int key)
    {
	    int a = -1;
		Random rnd = new Random();
	    
		a = rnd.Next(i);
	    while (dict.ContainsValue(dict[a]))
		    a = rnd.Next(i);
	    switch (key)
	    {
		    case 0:
			    rightKey = dict[a];
			    break;
		    case 1: 
			    leftKey = dict[a];
				break;
		    case 2: 
			    jumpKey = dict[a];
				break;
	    }
    }
    
    // Update is called once per frame
    void Update()
    {
	    _time += Time.deltaTime;
		CheckInput();
    }
    void FixedUpdate() 
    {
	    CheckCollisions();

	    HandleJump();
	    HandleDirection();
	    HandleGravity();
            
	    ApplyMovement();
	    checkHeight();
	    checkCamera();
    }

    private void CheckInput()
    {
	    float x = 0f;

	    if (Input.GetKey(leftKey))  x -= 1f;
	    if (Input.GetKey(rightKey)) x += 1f;
	    _input = new FrameInput
	    {
		    JumpDown = Input.GetKeyDown(jumpKey),
		    JumpHeld = Input.GetKey(jumpKey),
		    Move = new Vector2(x, Input.GetAxisRaw("Vertical"))
	    };
	    if (_stats.SnapInput)
	    {
		    _input.Move.x = Mathf.Abs(_input.Move.x) < _stats.HorizontalDeadZoneThreshold ? 0 : Mathf.Sign(_input.Move.x);
		    _input.Move.y = Mathf.Abs(_input.Move.y) < _stats.VerticalDeadZoneThreshold ? 0 : Mathf.Sign(_input.Move.y);
	    }

	    if (_input.JumpDown)
	    {
		    _jumpToConsume = true;
		    _timeJumpWasPressed = _time;
	    }
	    
    }
    
    // Collide 
    
    private float _frameLeftGround = float.MinValue;
    private bool _grounded;

    private void CheckCollisions()
    {
	    // empeche de se considerer soi meme dans un raycast (raycats commence dans lui meme)
	    Physics2D.queriesStartInColliders = false;
	    
	    bool groundHit = Physics2D.CapsuleCast(_col.bounds.center,_col.size, _col.direction, 0,  Vector2.down, _stats.GrounderDistance, ~_stats.PlayerLayer);
	    bool ceilingHit = Physics2D.CapsuleCast(_col.bounds.center,_col.size, _col.direction, 0,  Vector2.up, _stats.GrounderDistance, ~_stats.PlayerLayer);

	    if (ceilingHit)
		    _FrameVelocity.y = Mathf.Min(0, _FrameVelocity.y);

	    if (!_grounded && groundHit)
	    {
		    _grounded = true;
		    _coyoteUsable = true;
		    _bufferedJumpUsable = true;
		    _endedJumpEarly = true;
			// equivalene d'un if GroundChanged == null
		    GroundedChanged?.Invoke(true, Mathf.Abs(_FrameVelocity.y));
	    }
	    
	    else if (_grounded && !groundHit)
	    {
		    _grounded = false;
		    _frameLeftGround = _time;
		    GroundedChanged?.Invoke(false, 0);
	    }

	    Physics2D.queriesStartInColliders = _cachedQueryStartInColliders;
    }
    
    // jumping
    private bool _jumpToConsume;
    private bool _bufferedJumpUsable;
    private bool _endedJumpEarly;
    private bool _coyoteUsable;
    private float _timeJumpWasPressed;

    private bool HasBufferedJump => _bufferedJumpUsable && _time < _timeJumpWasPressed + _stats.JumpBuffer;
    private bool CanUseCoyote => _coyoteUsable && !_grounded && _time < _frameLeftGround + _stats.CoyoteTime;

    private void HandleJump()
    {
	    if (!_endedJumpEarly && !_grounded && !_input.JumpHeld && _rb.velocity.y > 0)
		    _endedJumpEarly =  true;
	    if (!_jumpToConsume && !HasBufferedJump) 
		    return;
		
	    if (_grounded || CanUseCoyote) ExcecuteJump();
	    
	    _jumpToConsume = false;
    }

    private void ExcecuteJump()
    {
	    _endedJumpEarly = false;
	    _timeJumpWasPressed = 0;
	    _bufferedJumpUsable = false;
	    _coyoteUsable = false;
	    _FrameVelocity.y = _stats.JumpPower;
	    Jumped?.Invoke();
    }

    private void HandleDirection()
    {
	    if (_input.Move.x == 0)
	    {
		    var deceleration = _grounded ? _stats.GroundDeceleration : _stats.AirDeceleration;
		    _FrameVelocity.x = Mathf.MoveTowards(_FrameVelocity.x, 0, deceleration * Time.deltaTime);
	    }
	    else
	    {
		    _FrameVelocity.x = Mathf.MoveTowards(_FrameVelocity.x, _input.Move.x * _stats.MaxSpeed, _stats.Acceleration * Time.deltaTime);
	    }
    }

    private void HandleGravity()
    {
	    if (_grounded && _FrameVelocity.y <= 0f)
		    _FrameVelocity.y = _stats.GroundingForce;
	    else
	    {
		    var inAirGravity = _stats.FallAcceleration;
		    if (_endedJumpEarly && _FrameVelocity.y > 0f)
			    inAirGravity *= _stats.JumpEndEarlyGravityModifier;
			_FrameVelocity.y = Mathf.MoveTowards(_FrameVelocity.y, -_stats.MaxFallSpeed, inAirGravity * Time.fixedDeltaTime);    
	    }
    }

    private void ApplyMovement() => _rb.velocity = _FrameVelocity;

	private void checkHeight()
	{
		if (_rb.position.y <= 0f)
			Spawn();
	}
    private void Spawn()
    {
	    _rb.MovePosition(new Vector2(_gm.position.x, _gm.position.y));
	    _rb.position = _gm.position;
    }

	public Camera cam;
	public float smoothSpeed = 5f;
	private float leftThreshold = 0.40f;
	private float rightThreshold = 0.60f;
	private float topThreshold = 0.60f;
	private float botThreshold = 0.40f;


    private void checkCamera()
    {
		Vector3 screenPos = cam.WorldToViewportPoint(_rb.position);
		float cameraWorldWidth = cam.orthographicSize * 2f * cam.aspect;

		if (screenPos.x > rightThreshold)
		{
			float deltaPercent = screenPos.x - rightThreshold;
			float deltaWorld = deltaPercent * cameraWorldWidth;
			Vector3 targetPos = cam.transform.position + new Vector3(deltaWorld, 0, 0);
			cam.transform.position = Vector3.Lerp(cam.transform.position, targetPos, Time.deltaTime * smoothSpeed);
		}
		else if (screenPos.x < leftThreshold)
		{
			float deltaPercent = screenPos.x - leftThreshold;
			float deltaWorld = deltaPercent * cameraWorldWidth;
			Vector3 targetPos = cam.transform.position + new Vector3(deltaWorld, 0, 0);
			cam.transform.position = Vector3.Lerp(cam.transform.position, targetPos, Time.deltaTime * smoothSpeed);
		}

		if (screenPos.y > topThreshold)
		{
			float deltaPercent = screenPos.y - topThreshold;
			float deltaWorld = deltaPercent * cameraWorldWidth;
			Vector3 targetPos = cam.transform.position + new Vector3(0, deltaWorld, 0);
			cam.transform.position = Vector3.Lerp(cam.transform.position, targetPos, Time.deltaTime * smoothSpeed);
		}
		else if (screenPos.y < botThreshold)
		{
			float deltaPercent = screenPos.y - botThreshold;
			float deltaWorld = deltaPercent * cameraWorldWidth;
			Vector3 targetPos = cam.transform.position + new Vector3(0, deltaWorld, 0);
			cam.transform.position = Vector3.Lerp(cam.transform.position, targetPos, Time.deltaTime * smoothSpeed);
		}

	}
}


