using Godot;
using System;
using System.Threading.Tasks;

public partial class Enemy : CharacterBody2D
{
	[Export] public float WalkSpeed = 60f;
	[Export] public float ChaseSpeed = 120f;
	[Export] public float PatrolDistance = 96f;
	[Export] public float AttackDmg = 33f;
	[Export] public float AttackCooldown = 1.0f;
	
	[Export] public float SlowMultiplier = 0.5f;
	[Export] public float SlowDuration = 1.5f;
	[Export] public int HitsToStun = 5;
	[Export] public float StunDuration = 2f;
	
	[Export] public PackedScene StunIndicatorScene;
	private StunIndicator _stunIndicator;

	private bool _isSlowed = false;
	private bool _isStunned = false;
	private int _sprayHitCount = 0;
	private int _slowToken = 0;
	
	private bool _attacking = false;
	private bool _canAttack = true;

	private AnimatedSprite2D _sprite;
	private Area2D _detectRange;
	private Area2D _attackRange;

	private movement _player;

	private Vector2 _spawnPos;
	private Vector2 _patrolTarget;

	private readonly Random _rng = new();

	private string _lastDirection = "S";

	private bool _playerInAttackRange = false;

	private enum State
	{
		Patrol,
		Chase,
		Attack,
		Stunned,
		Dead
	}

	private State _state = State.Patrol;
	
	private PlayerStats _playerStats;
	
	//----------------------------------------------------------

	public override void _Ready()
	{
		_sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");

		_detectRange = GetNode<Area2D>("DetectRange");
		_attackRange = GetNode<Area2D>("AttackRange");

		_detectRange.BodyEntered += OnDetectBodyEntered;
		_detectRange.BodyExited += OnDetectBodyExited;

		_attackRange.BodyEntered += OnAttackBodyEntered;
		_attackRange.BodyExited += OnAttackBodyExited;

		_sprite.AnimationFinished += OnAnimationFinished;

		_spawnPos = GlobalPosition;
		
		PickPatrolPoint();
	}

	//----------------------------------------------------------

	public override void _PhysicsProcess(double delta)
	{	
		if (_player != null && _player.IsDead)
		{
			_player = null;
			_playerStats = null;
			_playerInAttackRange = false;

			if (_state != State.Dead)
			{
				_state = State.Patrol;
				PickPatrolPoint();
			}
		}
		
		if (_player != null)
			UpdateLookAtPlayer();
		
		switch (_state)
		{
			case State.Patrol:
				Patrol();
				break;

			case State.Chase:
				Chase();
				break;

			case State.Attack:
				Velocity = Vector2.Zero;
				break;
			
			case State.Stunned:
				Velocity = Vector2.Zero;
				break;

			case State.Dead:
				Velocity = Vector2.Zero;
				break;
		}

		MoveAndSlide();
	}

	//----------------------------------------------------------
	// PATROL
	//----------------------------------------------------------

	private void Patrol()
	{	
		if (_isStunned)
		{
			Velocity = Vector2.Zero;
			return;
		}
		Vector2 dir = _patrolTarget - GlobalPosition;

		if (dir.Length() < 8)
		{
			PickPatrolPoint();
			PlayIdle();
			Velocity = Vector2.Zero;
			return;
		}

		dir = dir.Normalized();

		UpdateDirection(dir);

		if (TestMove(GlobalTransform, dir))
		{
			PickPatrolPoint();
			Velocity = Vector2.Zero;
			PlayIdle();
			return;
		}

		float speed = _isSlowed ? WalkSpeed * SlowMultiplier : WalkSpeed;
		Velocity = dir * speed;

		PlayWalk();
	}

	//----------------------------------------------------------
	// CHASE
	//----------------------------------------------------------

	private void Chase()
	{	
		if (_isStunned)
		{
			Velocity = Vector2.Zero;
			return;
		}
		if (_player == null || _player.IsDead)
		{
			_state = State.Patrol;
			PickPatrolPoint();
			return;
		}

		Vector2 toPlayer = _player.GlobalPosition - GlobalPosition;

		UpdateDirection(toPlayer.Normalized());

		if (toPlayer.Length() <= 10f)
		{
			Velocity = Vector2.Zero;
			PlayIdle();
			return;
		}

		Vector2 dir = toPlayer.Normalized();

		if (TestMove(GlobalTransform, dir))
		{
			Velocity = Vector2.Zero;
			PlayIdle();
			return;
		}

		float speed = _isSlowed
			? ChaseSpeed * SlowMultiplier
			: ChaseSpeed;

		Velocity = dir * speed;
		PlayWalk();
	}

	//----------------------------------------------------------
	// ATTACK
	//----------------------------------------------------------

	private async void StartAttack()
	{	
		if (_player == null || _player.IsDead)
		{
			_attacking = false;
			_canAttack = true;
			_state = State.Patrol;
			return;
		}
		if (_attacking || !_canAttack)
			return;

		_attacking = true;
		_canAttack = false;

		Velocity = Vector2.Zero;
		_state = State.Attack;

		_sprite.Play("Attack_" + _lastDirection);
		
		await ToSignal(GetTree().CreateTimer(0.4f), SceneTreeTimer.SignalName.Timeout);
		DamagePlayer();
			
		await ToSignal(GetTree().CreateTimer(0.4f), SceneTreeTimer.SignalName.Timeout);
		DamagePlayer();

		await ToSignal(_sprite, AnimatedSprite2D.SignalName.AnimationFinished);
		
		PlayIdle();

		await ToSignal(GetTree().CreateTimer(AttackCooldown),
					   SceneTreeTimer.SignalName.Timeout);

		_attacking = false;
		_canAttack = true;

		if (_player == null || _player.IsDead)
			_state = State.Patrol;
		else if (_playerInAttackRange)
			StartAttack();
		else
			_state = State.Chase;
	}

	//----------------------------------------------------------

	private void PickPatrolPoint()
	{
		float x = (float)(_rng.NextDouble() * PatrolDistance * 2 - PatrolDistance);
		float y = (float)(_rng.NextDouble() * PatrolDistance * 2 - PatrolDistance);

		_patrolTarget = _spawnPos + new Vector2(x, y);
	}

	//----------------------------------------------------------

	private void UpdateDirection(Vector2 dir)
	{
		if (Mathf.Abs(dir.X) > Mathf.Abs(dir.Y))
			_lastDirection = dir.X > 0 ? "E" : "W";
		else
			_lastDirection = dir.Y > 0 ? "S" : "N";
	}

	//----------------------------------------------------------

	private void PlayWalk()
	{
		string anim = "Walk_" + _lastDirection;

		if (_sprite.Animation != anim)
			_sprite.Play(anim);
	}

	private void PlayIdle()
	{
		string anim = "Idle_" + _lastDirection;

		if (_sprite.Animation != anim)
			_sprite.Play(anim);
	}

	//----------------------------------------------------------
	// DETECT
	//----------------------------------------------------------

	private void OnDetectBodyEntered(Node2D body)
	{
		if (body is movement player)
		{
			_player = player;
			_playerStats = player.GetNode<PlayerStats>("PlayerStats2");

			if (_state != State.Attack)
				_state = State.Chase;
		}
	}

	private void OnDetectBodyExited(Node2D body)
	{
		if (body != _player)
			return;

		_player = null;
		_playerInAttackRange = false;

		// Đang stun thì giữ nguyên stun
		if (_isStunned)
			return;

		if (_state != State.Attack)
		{
			_state = State.Patrol;
			PickPatrolPoint();
		}
	}

	//----------------------------------------------------------
	// ATTACK RANGE
	//----------------------------------------------------------

	private void OnAttackBodyEntered(Node2D body)
	{
		if (body != _player)
			return;

		_playerInAttackRange = true;

		if (_state != State.Attack)
			StartAttack();
	}

	private void OnAttackBodyExited(Node2D body)
	{
		if (body != _player)
			return;

		_playerInAttackRange = false;
	}

	//----------------------------------------------------------
	// ATTACK LOOP
	//----------------------------------------------------------

	private void OnAnimationFinished()
	{
		if (!_sprite.Animation.ToString().StartsWith("Attack"))
			return;

		PlayIdle();

		Velocity = Vector2.Zero;

		if (_player == null)
		{
			_state = State.Patrol;
			PickPatrolPoint();
			return;
		}

		if (!_playerInAttackRange)
		{
			_state = State.Chase;
		}
	}
	
	private void UpdateLookAtPlayer()
	{
		if (_player == null)
			return;

		Vector2 dir = (_player.GlobalPosition - GlobalPosition).Normalized();

		UpdateDirection(dir);
	}
	
	private void DamagePlayer()
	{	
		if (_player == null || _player.IsDead)
			return;
			
		if (!_playerInAttackRange)
			return;
		
		if (_playerStats != null)
			_playerStats.TakeDamage(AttackDmg);
	}
	
	public async void ApplyExtinguisherHit()
	{
		if (_state == State.Dead || _isStunned)
			return;

		_isSlowed = true;
		_sprayHitCount++;

		int token = ++_slowToken;

		if (_sprayHitCount >= HitsToStun)
		{
			_sprayHitCount = 0;
			await Stun();
			return;
		}

		await ToSignal(GetTree().CreateTimer(SlowDuration),
					   SceneTreeTimer.SignalName.Timeout);

		// Chỉ timer mới nhất mới được quyền hết slow
		if (token == _slowToken && !_isStunned)
			_isSlowed = false;
	}
	
	private async Task Stun()
	{
		_isStunned = true;
		_isSlowed = false;

		Velocity = Vector2.Zero;
		_state = State.Stunned;

		PlayIdle();
		
		if (StunIndicatorScene != null)
		{
			if (_stunIndicator != null &&
				!GodotObject.IsInstanceValid(_stunIndicator))
			{
				_stunIndicator = null;
			}

			if (_stunIndicator == null)
			{
				_stunIndicator = StunIndicatorScene.Instantiate<StunIndicator>();

				GetTree().CurrentScene.AddChild(_stunIndicator);

				_stunIndicator.Start(this, StunDuration);
			}
		}

		await ToSignal(GetTree().CreateTimer(StunDuration),
					   SceneTreeTimer.SignalName.Timeout);

		_isStunned = false;

		// Chỉ quyết định state sau khi stun kết thúc
		if (_player != null && !_player.IsDead)
			_state = State.Chase;
		else
		{
			_state = State.Patrol;
			PickPatrolPoint();
		}
	}
}
