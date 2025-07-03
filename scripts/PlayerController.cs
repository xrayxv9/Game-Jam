using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Random = UnityEngine.Random;


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



	void Awake()
	{
		_rb = GetComponent<Rigidbody2D>();
		_col = GetComponent<CapsuleCollider2D>();
		
		_cachedQueryStartInColliders = Physics2D.queriesStartInColliders;
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

    //void ChangeKeyBind(int key)
    //{
	//    int a = -1;
//	    switch (key):
//		    case 0:
//				Random rnd = new Random();
//			    a = rnd.Next(i);
//			    while (dict.ContainsKey(dict[a]))
//				    a = rnd.Next(i);
  //}
    
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
		
	    Debug.Log("grounded : " + _grounded + " Can Use coyote : " + CanUseCoyote);
	    if (_grounded || CanUseCoyote) ExcecuteJump();
	    
	    _jumpToConsume = false;
    }

    private void ExcecuteJump()
    {
	    Debug.Log("jumped");
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

    private void OnValidate()
    {
	    if (_stats == null)
		    Debug.LogWarning("Please assign a ScriptableStats asset to the Player Controller's Stats slot", this);
    }
}


