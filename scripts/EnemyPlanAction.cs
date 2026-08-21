using Godot;
using System.Collections.Generic;
using System.Globalization;

public struct EnemyPlanAction
{
	public char Direction;
	public float Duration;
}

// Chuyển chuỗi dạng "w 0.5 a 0.3 i 1.0 d 2.0" thành danh sách EnemyPlanAction.
// Tự lọc bỏ mọi token không hợp lệ thay vì ném lỗi — output của AI ngoài (LLM) KHÔNG được
// tin tưởng tuyệt đối, luôn phải validate/chặn giá trị bất thường trước khi cho Enemy thực thi.
public static class TacticalPlanParser
{
	private const float MaxSingleActionDuration = 25f;  // chặn AI ra lệnh giữ phím quá lâu 1 lần
	private const float MaxTotalPlanDuration = 45f;     // chặn tổng thời lượng kế hoạch quá dài

	public static List<EnemyPlanAction> Parse(string raw)
	{
		var actions = new List<EnemyPlanAction>();
		if (string.IsNullOrWhiteSpace(raw))
			return actions;

		var tokens = raw.Trim().Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
		float totalDuration = 0f;

		for (int i = 0; i + 1 < tokens.Length; i += 2)
		{
			string keyToken = tokens[i].Trim().ToLowerInvariant();
			if (keyToken.Length != 1) continue;

			char key = keyToken[0];
			if (key != 'w' && key != 'a' && key != 's' && key != 'd' && key != 'i')
				continue;

			if (!float.TryParse(tokens[i + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out float duration))
				continue;

			duration = Mathf.Clamp(duration, 0f, MaxSingleActionDuration);
			if (totalDuration + duration > MaxTotalPlanDuration)
				duration = Mathf.Max(0f, MaxTotalPlanDuration - totalDuration);

			if (duration <= 0f) continue;

			actions.Add(new EnemyPlanAction { Direction = key, Duration = duration });
			totalDuration += duration;

			if (totalDuration >= MaxTotalPlanDuration) break;
		}

		return actions;
	}

	public static float TotalDuration(List<EnemyPlanAction> plan)
	{
		float total = 0f;
		foreach (var a in plan) total += a.Duration;
		return total;
	}
}
