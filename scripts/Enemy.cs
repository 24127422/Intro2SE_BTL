using Godot;
using System;

public partial class Enemy : CharacterBody2D
{
	[Export] public float WalkSpeed = 60f;
	[Export] public float ChaseSpeed = 120f;
	[Export] public float PatrolDistance = 96f;
	[Export] public float AttackCooldown = 1.0f;
	
	private bool _attacking = false;
	private bool _canAttack = true;

	private AnimatedSprite2D _sprite;
	private Area2D _detectRange;
	private Area2D _attackRange;

	private Movement _player;

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
		Dead
	}

	private State _state = State.Patrol;

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

		Velocity = dir * WalkSpeed;

		PlayWalk();
	}

	//----------------------------------------------------------
	// CHASE
	//----------------------------------------------------------

	private void Chase()
	{
		if (_player == null)
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

		Velocity = dir * ChaseSpeed;
		PlayWalk();
	}

	//----------------------------------------------------------
	// ATTACK
	//----------------------------------------------------------

	private async void StartAttack()
	{
		if (_attacking || !_canAttack)
			return;

		_attacking = true;
		_canAttack = false;

		Velocity = Vector2.Zero;
		_state = State.Attack;

		_sprite.Play("Attack_" + _lastDirection);

		await ToSignal(_sprite, AnimatedSprite2D.SignalName.AnimationFinished);

		PlayIdle();

		await ToSignal(GetTree().CreateTimer(AttackCooldown),
					   SceneTreeTimer.SignalName.Timeout);

		_attacking = false;
		_canAttack = true;

		if (_player == null)
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
		if (body is Movement player)
		{
			_player = player;

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
}
