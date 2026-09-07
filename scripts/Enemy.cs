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
	[Export] public int RequiredExtinguisherHits = 5;
	[Export] public float ExtinguisherHitResetTime = 4f;
	[Export] public float StunDuration = 3f;
	
	private int _extinguisherHitCount = 0;
	private float _extinguisherHitResetTimer = 0f;

	[Export] public int PatrolDirectionSamples = 16;
	[Export] public float PatrolProbeStep = 16f;

	// === Zone patrol route (Groq) ===
	[Export] public float ZoneRouteRequestInterval = 120f;
	[Export] public float ZoneRouteFirstDelay = 15f;
	[Export] public int ZonePatrolRouteLength = 4;
	[Export] public float ZoneWaypointArrivalThreshold = 12f;
	private float _zoneRouteCooldown;

	private List<string> _currentZoneRoute = new();
	private int _zoneRouteIndex = 0;
	private bool _hasZoneRoute = false;

	// Đang đứng chờ Door.cs tự dịch chuyển (đã đi tới cửa/cầu thang, không tự bước tiếp nữa)
	private bool _awaitingDoorTeleport = false;
	public bool IsAwaitingDoorTeleport => _awaitingDoorTeleport;

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
	
	private string _lastUsedTransitionName = null;
	private string _pendingDoorTransitionName = null;
	
	// Sau khi vừa được Door.cs teleport vào 1 zone, enemy bắt buộc phải tự đi bộ
	// tới waypoint riêng của chính zone đó trước, rồi mới được xét hop kế tiếp
	// trong route. Tránh việc enemy đứng nguyên tại điểm hạ cánh sát mép cửa
	// (dễ bị coi là còn trong vùng Area2D của cửa vừa dùng và bị kéo ngược lại).
	private bool _awaitingZoneArrival = false;
	private string _pendingZoneArrivalName = null;

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

		_zoneRouteCooldown = ZoneRouteFirstDelay;
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

		_zoneRouteCooldown -= (float)delta;
		if (_zoneRouteCooldown <= 0f)
		{
			_zoneRouteCooldown = ZoneRouteRequestInterval;
			RequestZonePatrolRoute();
		}

		switch (_state)
		{
			case State.Patrol:
				if (_hasZoneRoute)
					MoveAlongZoneRoute();
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

	// === ZONE PATROL ROUTE (gọi Groq, fallback thống kê nếu lỗi) ===

	private async void RequestZonePatrolRoute()
	{
		if (_state != State.Patrol) return;

		string currentZone = ZoneRegistry.Instance?.GetNearestZone(GlobalPosition);
		if (currentZone == null) return;

		var stats = ZoneStatsTracker.Instance?.GetStats() ?? new Dictionary<string, float>();

		var route = await GroqZoneRouteAI.RequestRouteAsync(currentZone, stats, ZonePatrolRouteLength);

		if (!GodotObject.IsInstanceValid(this)) return;
		if (_state != State.Patrol) return;

		if (route == null || route.Count < 2)
		{
			route = ZonePatrolPlanner.GenerateRoute(currentZone, stats, ZonePatrolRouteLength, _rng);
			GD.Print($"[Enemy:{Name}] Groq fail -> fallback route thống kê: {string.Join(" -> ", route)}");
		}
		else
		{
			GD.Print($"[Enemy:{Name}] Route từ Groq: {string.Join(" -> ", route)}");
		}

		if (route.Count < 2) return;

		_currentZoneRoute = route;
		_zoneRouteIndex = 1;
		_hasZoneRoute = true;
		_awaitingDoorTeleport = false;
		_lastUsedTransitionName = null;
		_pendingDoorTransitionName = null;
		_awaitingZoneArrival = false;
		_pendingZoneArrivalName = null;
	}

	// Tên transition (door/stair) cần dùng cho bước hiện tại (từ zone[i-1] -> zone[i]).
	// Null nghĩa là 2 zone đi bộ trực tiếp được, không cần qua cửa/cầu thang.
	private string GetCurrentHopTransitionName()
	{
		if (_zoneRouteIndex <= 0 || _zoneRouteIndex >= _currentZoneRoute.Count) return null;
		string from = _currentZoneRoute[_zoneRouteIndex - 1];
		string to = _currentZoneRoute[_zoneRouteIndex];
		return ZoneGraph.GetTransitionName(from, to);
	}

	// Vị trí cần đi tới cho bước hiện tại: hoặc là cửa/cầu thang, hoặc là waypoint của zone đích
	private Vector2? GetCurrentHopTargetPosition(string transitionName)
	{
		if (_zoneRouteIndex >= _currentZoneRoute.Count) return null;

		if (transitionName != null)
			return TransitionRegistry.Instance?.GetWaypoint(transitionName);

		return ZoneRegistry.Instance?.GetWaypoint(_currentZoneRoute[_zoneRouteIndex]);
	}

	private void MoveAlongZoneRoute()
	{
		if (_awaitingDoorTeleport)
		{
			Velocity = Vector2.Zero;
			PlayIdle();
			return;
		}

		// Bắt buộc đi vào tới waypoint riêng của zone vừa teleport vào, trước khi
		// được phép xét hop kế tiếp trong route (kể cả hop đó có cần qua cửa khác
		// hay không). Đảm bảo enemy luôn thực sự rời khỏi vùng cửa vừa dùng.
		if (_awaitingZoneArrival)
		{
			Vector2? zoneWaypoint = ZoneRegistry.Instance?.GetWaypoint(_pendingZoneArrivalName);

			if (zoneWaypoint == null)
			{
				_awaitingZoneArrival = false;
				_pendingZoneArrivalName = null;
				AdvanceZoneRouteHop();
				return;
			}

			Vector2 toZoneWaypoint = zoneWaypoint.Value - GlobalPosition;

			if (toZoneWaypoint.Length() <= ZoneWaypointArrivalThreshold)
			{
				_awaitingZoneArrival = false;
				_pendingZoneArrivalName = null;
				AdvanceZoneRouteHop();
				return;
			}

			Vector2 arrivalMoveDir = ResolveMoveDirection(toZoneWaypoint.Normalized());
			if (arrivalMoveDir == Vector2.Zero) { Velocity = Vector2.Zero; PlayIdle(); return; }

			UpdateDirection(arrivalMoveDir);
			Velocity = arrivalMoveDir * WalkSpeed;
			PlayWalk();
			return;
		}

		string transitionName = GetCurrentHopTransitionName();

		bool isImmediateUTurn = transitionName != null && transitionName == _lastUsedTransitionName;
		if (transitionName != null)
			_lastUsedTransitionName = null;

		if (isImmediateUTurn)
		{
			GD.Print($"[Enemy:{Name}] Route boomerang qua cửa '{transitionName}' -> huỷ route, patrol random.");
			_hasZoneRoute = false;
			PickPatrolPoint();
			return;
		}

		Vector2? targetPos = GetCurrentHopTargetPosition(transitionName);

		if (targetPos == null)
		{
			_hasZoneRoute = false;
			PickPatrolPoint();
			return;
		}

		Vector2 toTarget = targetPos.Value - GlobalPosition;

		if (toTarget.Length() <= ZoneWaypointArrivalThreshold)
		{
			if (transitionName != null)
			{
				_awaitingDoorTeleport = true;
				_pendingDoorTransitionName = transitionName;
				Velocity = Vector2.Zero;
				PlayIdle();
				return;
			}

			AdvanceZoneRouteHop();
			return;
		}

		Vector2 moveDir = ResolveMoveDirection(toTarget.Normalized());
		if (moveDir == Vector2.Zero) { Velocity = Vector2.Zero; PlayIdle(); return; }

		UpdateDirection(moveDir);
		Velocity = moveDir * WalkSpeed;
		PlayWalk();
	}

	private void AdvanceZoneRouteHop()
	{
		_zoneRouteIndex++;
		if (_zoneRouteIndex >= _currentZoneRoute.Count)
		{
			_hasZoneRoute = false;
			PickPatrolPoint();
		}
	}

	// Được Door.cs gọi ngược lại sau khi đã dịch chuyển enemy qua cửa/cầu thang xong
	public void OnDoorTeleported()
	{
		_awaitingDoorTeleport = false;
		_lastUsedTransitionName = _pendingDoorTransitionName;
		_pendingDoorTransitionName = null;

		string arrivedZone = (_zoneRouteIndex >= 0 && _zoneRouteIndex < _currentZoneRoute.Count)
			? _currentZoneRoute[_zoneRouteIndex]
			: null;

		if (arrivedZone != null && ZoneRegistry.Instance?.GetWaypoint(arrivedZone) != null)
		{
			_pendingZoneArrivalName = arrivedZone;
			_awaitingZoneArrival = true;
		}
		else
		{
			AdvanceZoneRouteHop();
		}
	}

	private void AbandonZoneRoute()
	{
		_hasZoneRoute = false;
		_awaitingDoorTeleport = false;
		_lastUsedTransitionName = null;
		_pendingDoorTransitionName = null;
		_awaitingZoneArrival = false;
		_pendingZoneArrivalName = null;
	}
	
	private int _avoidSide = 0;
	
	private static readonly float[] _avoidAngleMagnitudesDeg =
	{
		15f, 30f, 45f, 60f, 75f, 90f, 105f, 120f, 135f, 150f, 165f, 180f
	};

	// Thử đi thẳng tới đích trước; nếu bị chặn thì trượt theo từng trục riêng
	private Vector2 ResolveMoveDirection(Vector2 desiredDir)
	{
		desiredDir = desiredDir.Normalized();

		// Đi thẳng được -> luôn ưu tiên, đồng thời huỷ trạng thái đang né.
		if (!TestMove(GlobalTransform, desiredDir))
		{
			_avoidSide = 0;
			return desiredDir;
		}

		// Đang né về 1 phía rồi thì ưu tiên tiếp tục đúng phía đó trước
		// (không xét phía ngược lại) để không bị giật qua lại.
		if (_avoidSide != 0)
		{
			foreach (float mag in _avoidAngleMagnitudesDeg)
			{
				Vector2 candidate = desiredDir.Rotated(Mathf.DegToRad(mag * _avoidSide));
				if (!TestMove(GlobalTransform, candidate))
					return candidate;
			}

			// Phía đang né bị bịt hoàn toàn -> bỏ khoá, quét lại từ đầu (có thể đổi phía)
			_avoidSide = 0;
		}

		// Chưa né phía nào (hoặc vừa mất phía cũ) -> quét cả 2 phía, phía nào thoáng
		// trước ở mức lệch nhỏ nhất thì chọn và khoá luôn phía đó.
		foreach (float mag in _avoidAngleMagnitudesDeg)
		{
			foreach (int side in new[] { 1, -1 })
			{
				Vector2 candidate = desiredDir.Rotated(Mathf.DegToRad(mag * side));
				if (!TestMove(GlobalTransform, candidate))
				{
					_avoidSide = side;
					return candidate;
				}
			}
		}

		return Vector2.Zero;
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

		if (toPlayer.Length() <= 10f)
		{
			Velocity = Vector2.Zero;
			PlayIdle();
			return;
		}

		Vector2 desiredDir = toPlayer.Normalized();

		Vector2 moveDir = ResolveMoveDirection(desiredDir);

		if (moveDir == Vector2.Zero)
		{
			Velocity = Vector2.Zero;
			PlayIdle();
			return;
		}

		Velocity = moveDir * ChaseSpeed;
		PlayWalk();
	}

	private async void StartAttack()
	{
		if (_state == State.Stunned) return;

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

		if (_state == State.Stunned)
		{
			// Đã đang stun rồi -> xịt thêm chỉ để gia hạn thời gian stun, không tính vào bộ đếm hit mới.
			_stunTimeRemaining = StunDuration;
			_activeStunIndicator?.Start(this, StunDuration);
			return;
		}

		// Chưa bị stun -> mỗi lần xịt cộng dồn 1 hit, đủ RequiredExtinguisherHits lần mới thực sự stun.
		_extinguisherHitCount++;
		_extinguisherHitResetTimer = ExtinguisherHitResetTime;

		if (_extinguisherHitCount >= RequiredExtinguisherHits)
		{
			_extinguisherHitCount = 0;
			_extinguisherHitResetTimer = 0f;
			BeginStun();
		}
	}

	private void BeginStun()
	{
		if (_hasZoneRoute)
			AbandonZoneRoute();

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
		_activeStunIndicator = null;

		if (_player != null && !_player.IsDead)
		{
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
		Vector2 dir = PickWeightedRandomDirection(PatrolDirectionSamples, PatrolProbeStep, PatrolDistance);

		if (dir == Vector2.Zero)
		{
			_patrolTarget = GlobalPosition;
			return;
		}

		float dist = ProbeClearDistance(dir, PatrolDistance, PatrolProbeStep);
		dist = Mathf.Max(0f, dist - PatrolProbeStep * 0.5f);

		_patrolTarget = GlobalPosition + dir * dist;
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

			if (_hasZoneRoute)
				AbandonZoneRoute();

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

	private float ProbeClearDistance(Vector2 dir, float maxDistance, float step)
	{
		dir = dir.Normalized();
		float traveled = 0f;
		var transform = GlobalTransform;

		while (traveled < maxDistance)
		{
			float moveStep = Mathf.Min(step, maxDistance - traveled);
			Vector2 testOffset = dir * moveStep;

			if (TestMove(transform, testOffset))
				break;

			transform.Origin += testOffset;
			traveled += moveStep;
		}

		return traveled;
	}

	private Vector2 PickWeightedRandomDirection(int sampleCount, float step, float maxDistance)
	{
		var candidates = new List<(Vector2 dir, float dist)>();

		for (int i = 0; i < sampleCount; i++)
		{
			float angle = (float)(_rng.NextDouble() * Mathf.Tau);
			Vector2 dir = Vector2.Right.Rotated(angle);

			float dist = ProbeClearDistance(dir, maxDistance, step);
			if (dist > step)
				candidates.Add((dir, dist));
		}

		if (candidates.Count == 0)
			return Vector2.Zero;

		float totalWeight = 0f;
		foreach (var c in candidates)
			totalWeight += c.dist * c.dist;

		float roll = (float)_rng.NextDouble() * totalWeight;
		float cumulative = 0f;
		foreach (var c in candidates)
		{
			cumulative += c.dist * c.dist;
			if (roll <= cumulative)
				return c.dir;
		}

		return candidates[^1].dir;
	}
	
	public void NotifyEnteredDoorZone(string transitionName)
	{
		if (_state != State.Patrol || !_hasZoneRoute) return;
		if (GetCurrentHopTransitionName() != transitionName) return;

		_awaitingDoorTeleport = true;
		Velocity = Vector2.Zero;
	}
	
	public void NotifyExitedDoorZone(string transitionName)
	{
		if (!_awaitingDoorTeleport) return;
		if (GetCurrentHopTransitionName() != transitionName) return;

		_awaitingDoorTeleport = false;
	}
}
