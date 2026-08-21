using Godot;
using System;
using System.Collections.Generic;

public partial class Enemy : CharacterBody2D
{
	[Export] public float WalkSpeed = 60f;
	[Export] public float ChaseSpeed = 120f;
	[Export] public float PatrolDistance = 96f;
	[Export] public float AttackDmg = 33f;
	[Export] public float AttackCooldown = 1.0f;

	[Export] public PackedScene StunIndicatorScene;
	[Export] public float StunDuration = 3f;

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

	private float _stunTimeRemaining = 0f;
	private StunIndicator _activeStunIndicator;

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

	// train_tactical_ai.py + EnemyNeuralPolicy.cs). KHÔNG cần gọi API/HTTP/GDExtension
	// nào cả — forward pass chạy inline ngay trong Enemy, mỗi TacticalRequestInterval giây. ===
	[Export] public float TacticalRequestInterval = 120f;
	private float _tacticalCooldown;

	private List<EnemyPlanAction> _tacticalPlan = new();
	private int _planIndex = 0;
	private float _actionElapsed = 0f;
	private float _planElapsedTotal = 0f;
	private float _planTotalDuration = 0f;
	private Vector2 _planStartPosition;
	private bool _hasActivePlan = false;

	public string CurrentStateName => _state.ToString();

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

		_tacticalCooldown = TacticalRequestInterval;
	}

	public override void _PhysicsProcess(double delta)
	{	
		if (_player != null && _player.IsDead)
		{
			_player = null;
			_playerStats = null;
			_playerInAttackRange = false;

			if (_state != State.Dead && _state != State.Stunned)
			{
				_state = State.Patrol;
				PickPatrolPoint();
			}
		}
		
		if (_player != null)
			UpdateLookAtPlayer();

		_tacticalCooldown -= (float)delta;
		if (_tacticalCooldown <= 0f)
		{
			_tacticalCooldown = TacticalRequestInterval;
			RequestTacticalPlan();
		}

		switch (_state)
		{
			case State.Patrol:
				if (_hasActivePlan)
					ExecuteTacticalPlan((float)delta);
				else
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
				_stunTimeRemaining -= (float)delta;
				if (_stunTimeRemaining <= 0f)
					EndStun();
				break;

			case State.Dead:
				Velocity = Vector2.Zero;
				break;
		}

		MoveAndSlide();
	}


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

	// Gọi mỗi TacticalRequestInterval giây. Chỉ lập kế hoạch mới khi rảnh (không đang
	// combat/chết/choáng) VÀ đã từng "thấy" player ít nhất 1 lần
	// không có state player thì không có gì để dự đoán/đón đầu.
	private async void RequestTacticalPlan()
{
	if (_state == State.Attack || _state == State.Dead || _state == State.Stunned) return;

	var player = movement.Instance;
	if (player == null || !GodotObject.IsInstanceValid(player) || player.IsDead) return;

	var playerStats = player.GetNodeOrNull<PlayerStats>("PlayerStats2");

	Vector2 rel = player.GlobalPosition - GlobalPosition;
	float dist = rel.Length();
	float hpPercent = playerStats != null ? playerStats.HealthPercent : 100f;

	bool wasPatrol = _state == State.Patrol;
	bool wasChase = _state == State.Chase;

	var plan = await GroqTacticalAI.RequestPlanAsync(
		relX: rel.X, relY: rel.Y,
		playerVelX: player.Velocity.X, playerVelY: player.Velocity.Y,
		dist: dist, hpPercent: hpPercent,
		isPatrol: wasPatrol, isChase: wasChase
	);

	if (!GodotObject.IsInstanceValid(this)) return;
	if (_state == State.Attack || _state == State.Dead || _state == State.Stunned) return;

	if (plan == null || plan.Count == 0)
	{
		plan = EnemyNeuralPolicy.Predict(
			rel.X, rel.Y, player.Velocity.X, player.Velocity.Y,
			dist, hpPercent, wasPatrol, wasChase
		);
	}

	AssignTacticalPlan(plan);
}

	// Được RequestTacticalPlan() gọi sau khi mạng dự đoán xong.
	// Không áp dụng nếu quái đang giao tranh hoặc đang choáng — combat/stun luôn được
	// ưu tiên hơn kế hoạch chiến thuật.
	public void AssignTacticalPlan(List<EnemyPlanAction> plan)
	{
		if (_state == State.Attack || _state == State.Dead || _state == State.Stunned) return;
		if (plan == null || plan.Count == 0) return;

		_tacticalPlan = plan;
		_planIndex = 0;
		_actionElapsed = 0f;
		_planElapsedTotal = 0f;
		_planTotalDuration = TacticalPlanParser.TotalDuration(plan);
		_planStartPosition = GlobalPosition;
		_hasActivePlan = true;

		GD.Print($"[Enemy:{Name}] Nhận path mới — {plan.Count} bước, tổng {_planTotalDuration:F2}s: {FormatPlan(plan)}");

		_state = State.Patrol;
	}

	private string FormatPlan(List<EnemyPlanAction> plan)
	{
		var parts = new List<string>();
		foreach (var action in plan)
			parts.Add($"{action.Direction}({action.Duration:F2}s)");
		return string.Join(" -> ", parts);
	}

	private void ExecuteTacticalPlan(float delta)
{
	_planElapsedTotal += delta;

	// Kiểm tra hoàn thành trước — nếu plan đã chạy hết các bước thì không bao giờ rollback.
	if (_planIndex >= _tacticalPlan.Count)
	{
		_hasActivePlan = false;
		PickPatrolPoint();
		return;
	}

	const float RollbackGraceMargin = 5f;
	if (_planElapsedTotal > _planTotalDuration + RollbackGraceMargin)
	{
		RollbackTacticalPlan();
		return;
	}

	var action = _tacticalPlan[_planIndex];

	if (_actionElapsed <= 0f)
	{
		GD.Print($"[Enemy:{Name}] Đang đi hướng '{action.Direction}' trong {action.Duration:F2}s (bước {_planIndex + 1}/{_tacticalPlan.Count})");
	}

	_actionElapsed += delta;

	Vector2 dir = action.Direction switch
	{
		'w' => Vector2.Up,
		's' => Vector2.Down,
		'a' => Vector2.Left,
		'd' => Vector2.Right,
		_ => Vector2.Zero,
	};

	if (dir == Vector2.Zero)
	{
		Velocity = Vector2.Zero;
		PlayIdle();
	}
	else
	{
		UpdateDirection(dir);

		if (TestMove(GlobalTransform, dir))
		{
			Velocity = Vector2.Zero;
			PlayIdle();
		}
		else
		{
			Velocity = dir * WalkSpeed;
			PlayWalk();
		}
	}

	if (_actionElapsed >= action.Duration)
	{
		_actionElapsed = 0f;
		_planIndex++;
	}
}

	private void RollbackTacticalPlan()
	{
		GD.Print("[Enemy] Kế hoạch chiến thuật vượt thời gian dự tính -> rollback vị trí.");
		GlobalPosition = _planStartPosition;
		Velocity = Vector2.Zero;
		_tacticalPlan.Clear();
		_planIndex = 0;
		_hasActivePlan = false;
		PickPatrolPoint();
	}

	// Huỷ kế hoạch giữa chừng nhưng KHÔNG rollback vị trí — dùng khi combat (Chase/Attack)
	// chen ngang, vì lúc đó "tua ngược" quái giữa giao tranh sẽ trông như lỗi hiển thị.
	private void AbandonTacticalPlan()
	{
		_tacticalPlan.Clear();
		_planIndex = 0;
		_hasActivePlan = false;
	}

	private void Chase()
	{
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

		Velocity = dir * ChaseSpeed;
		PlayWalk();
	}

	private async void StartAttack()
	{	
		if (_state == State.Stunned) return; // an toàn: không đánh khi đang choáng

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

		// Nếu bị bình cứu hỏa xịt choáng ngay trong lúc đang hồi chiêu ở trên, để
		// EndStun() tự quyết định state tiếp theo, không ghi đè state ở đây nữa.
		if (_state == State.Stunned)
			return;

		if (_player == null || _player.IsDead)
			_state = State.Patrol;
		else if (_playerInAttackRange)
			StartAttack();
		else
			_state = State.Chase;
	}

	public void ApplyExtinguisherHit()
	{
		if (_state == State.Dead) return;

		if (_state != State.Stunned)
		{
			BeginStun();
		}
		else
		{
			_stunTimeRemaining = StunDuration;
			_activeStunIndicator?.Start(this, StunDuration);
		}
	}

	private void BeginStun()
	{
		if (_hasActivePlan)
			AbandonTacticalPlan();

		_attacking = false;
		_state = State.Stunned;
		_stunTimeRemaining = StunDuration;

		Velocity = Vector2.Zero;
		PlayIdle();

		if (StunIndicatorScene != null)
		{
			_activeStunIndicator = StunIndicatorScene.Instantiate<StunIndicator>();
			GetTree().CurrentScene.AddChild(_activeStunIndicator);
			_activeStunIndicator.Start(this, StunDuration);
		}
	}

	private void EndStun()
	{
		_stunTimeRemaining = 0f;
		_activeStunIndicator = null; // StunIndicator tự QueueFree() khi hết thời gian của chính nó

		if (_player != null && !_player.IsDead)
		{
			// Đặt về Chase TRƯỚC khi gọi StartAttack(), vì StartAttack() tự early-return
			// nếu thấy _state vẫn còn là State.Stunned.
			_state = State.Chase;
			if (_playerInAttackRange)
				StartAttack();
		}
		else
		{
			_state = State.Patrol;
			PickPatrolPoint();
		}
	}


	private void PickPatrolPoint()
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

	private void OnDetectBodyEntered(Node2D body)
	{
		if (body is movement player)
		{
			_player = player;
			_playerStats = player.GetNode<PlayerStats>("PlayerStats2");

			// === MỚI: combat luôn được ưu tiên hơn kế hoạch AI ngoài ===
			if (_hasActivePlan)
				AbandonTacticalPlan();

			if (_state != State.Attack && _state != State.Stunned)
				_state = State.Chase;
		}
	}

	private void OnDetectBodyExited(Node2D body)
	{
		if (body != _player)
			return;

		_player = null;
		_playerInAttackRange = false;

		if (_state != State.Attack && _state != State.Stunned)
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

		if (_state != State.Attack && _state != State.Stunned)
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
		if (_state == State.Stunned)
			return;

		if (_player == null || _player.IsDead)
			return;
			
		if (!_playerInAttackRange)
			return;
		
		if (_playerStats != null)
			_playerStats.TakeDamage(AttackDmg);
	}
}
