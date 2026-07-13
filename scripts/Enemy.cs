using Godot;
using System;

public partial class Enemy : CharacterBody2D
{
	[Export] public float WalkSpeed = 70f;
	[Export] public float ChaseSpeed = 120f;
	[Export] public float PatrolDistance = 96f;

	private AnimatedSprite2D _sprite;
	private Area2D _detectRange;
	private Area2D _attackRange;
	private Timer _attackCD;

	private CharacterBody2D _player;

	private enum State
	{
		Patrol,
		Chase,
		Attack,
		Dead
	}

	private State _state = State.Patrol;

	private string _lastDirection = "S";

	private Vector2 _spawnPos;
	private Vector2 _patrolTarget;

	private Random _rng = new Random();

	public override void _Ready()
	{
		_sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");

		_detectRange = GetNode<Area2D>("DetectRange");
		_attackRange = GetNode<Area2D>("AttackRange");
		_attackCD = GetNode<Timer>("AttackCD");

		_spawnPos = GlobalPosition;
		PickNewPatrolPoint();

		_detectRange.BodyEntered += OnDetectBodyEntered;
		_detectRange.BodyExited += OnDetectBodyExited;

		_attackRange.BodyEntered += OnAttackBodyEntered;
		_attackRange.BodyExited += OnAttackBodyExited;
	}

	public override void _PhysicsProcess(double delta)
	{
		switch (_state)
		{
			case State.Patrol:
				Patrol();
				break;

			case State.Chase:
				ChasePlayer();
				break;

			case State.Attack:
				AttackPlayer();
				break;

			case State.Dead:
				Velocity = Vector2.Zero;
				break;
		}

		MoveAndSlide();
	}

	private void Patrol()
	{
		Vector2 dir = (_patrolTarget - GlobalPosition);

		if (dir.Length() < 4)
		{
			PickNewPatrolPoint();
			return;
		}

		dir = dir.Normalized();

		UpdateDirection(dir);

		Velocity = dir * WalkSpeed;

		PlayWalk();
	}

	private void ChasePlayer()
	{
		if (_player == null)
		{
			_state = State.Patrol;
			return;
		}

		Vector2 dir = (_player.GlobalPosition - GlobalPosition).Normalized();

		UpdateDirection(dir);

		Velocity = dir * ChaseSpeed;

		PlayWalk();
	}

	private void AttackPlayer()
	{
		Velocity = Vector2.Zero;

		PlayAttack();

		if (_attackCD.IsStopped())
		{
			_attackCD.Start();

			// TODO:
			// player.TakeDamage(...)
		}
	}

	private void PickNewPatrolPoint()
	{
		float x = (float)(_rng.NextDouble() * PatrolDistance * 2 - PatrolDistance);
		float y = (float)(_rng.NextDouble() * PatrolDistance * 2 - PatrolDistance);

		_patrolTarget = _spawnPos + new Vector2(x, y);
	}

	private void UpdateDirection(Vector2 dir)
	{
		if (Mathf.Abs(dir.X) > Mathf.Abs(dir.Y))
			_lastDirection = dir.X > 0 ? "E" : "W";
		else
			_lastDirection = dir.Y > 0 ? "S" : "N";
	}

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

	private void PlayAttack()
	{
		string anim = "Attack_" + _lastDirection;

		if (_sprite.Animation != anim)
			_sprite.Play(anim);
	}
	
	private void OnDetectBodyEntered(Node2D body)
	{
		if (body is movement player)
		{
			_player = player;
			_state = State.Chase;
		}
	}

	private void OnDetectBodyExited(Node2D body)
	{
		if (body == _player)
		{
			_player = null;
			_state = State.Patrol;
			PlayIdle();
		}
	}

	private void OnAttackBodyEntered(Node2D body)
	{
		if (body == _player)
		{
			_state = State.Attack;
		}
	}

	private void OnAttackBodyExited(Node2D body)
	{
		if (body == _player)
		{
			_state = State.Chase;
		}
	}
}
