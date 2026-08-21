using Godot;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using HttpClient = System.Net.Http.HttpClient;
using StringContent = System.Net.Http.StringContent;

public static class GroqTacticalAI
{
	private static readonly HttpClient _http = new HttpClient
	{
		Timeout = TimeSpan.FromSeconds(6)
	};

	private const string Url = "https://api.groq.com/openai/v1/chat/completions";
	private const string Model = "openai/gpt-oss-120b";

	public static async Task<List<EnemyPlanAction>> RequestPlanAsync(
		float relX, float relY,
		float playerVelX, float playerVelY,
		float dist, float hpPercent,
		bool isPatrol, bool isChase)
	{
		string apiKey = GroqConfig.Instance?.ApiKey;
		if (string.IsNullOrEmpty(apiKey))
		{
			GD.PrintErr("[GroqTacticalAI] Không có API key -> bỏ qua, để Enemy tự fallback.");
			return new List<EnemyPlanAction>();
		}

		string prompt = BuildPrompt(relX, relY, playerVelX, playerVelY, dist, hpPercent, isPatrol, isChase);

		var payload = new
		{
			model = Model,
			messages = new[]
			{
				new { role = "user", content = prompt }
			},
			temperature = 0.8,
			max_tokens = 400,
			reasoning_effort = "low"
		};

		try
		{
			var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Post, Url);
			request.Headers.Add("Authorization", $"Bearer {apiKey}");
			request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

			var response = await _http.SendAsync(request);

			if (!response.IsSuccessStatusCode)
			{
				string errorBody = await response.Content.ReadAsStringAsync();
				GD.PrintErr($"[GroqTacticalAI] HTTP {response.StatusCode}");
				GD.PrintErr($"[GroqTacticalAI] Chi tiết lỗi: {errorBody}");
				return new List<EnemyPlanAction>();
			}

			string body = await response.Content.ReadAsStringAsync();
			string raw = ExtractText(body);

			GD.Print($"[GroqTacticalAI] Raw text từ Groq: '{raw}'");

			return TacticalPlanParser.Parse(raw);
		}
		catch (Exception ex)
		{
			GD.PrintErr($"[GroqTacticalAI] Lỗi gọi API: {ex.Message}");
			return new List<EnemyPlanAction>();
		}
	}

	private static string BuildPrompt(
		float relX, float relY, float playerVelX, float playerVelY,
		float dist, float hpPercent, bool isPatrol, bool isChase)
	{
		return
			"Bạn là AI điều khiển 1 kẻ địch trong game 2D top-down horror.\n" +
			"Mục tiêu: đưa ra chuỗi di chuyển để tiếp cận gần vị trí người chơi (dựa vào relX, relY).\n" +
			"CHỈ trả lời theo đúng format sau, không giải thích gì thêm, không markdown:\n" +
			"'<phím> <giây> <phím> <giây> ...' — có thể dùng NHIỀU bước nối tiếp nhau, không giới hạn số cặp,\n" +
			"miễn tổng thời lượng toàn bộ chuỗi không vượt quá 45 giây.\n" +
			"phím chỉ được là 1 trong: w a s d i (i = đứng im)\n" +
			"giây là số thực từ 0.1 đến 25.0 — dùng thời lượng đủ dài và đủ số bước để thực sự tới gần\n" +
			"vị trí người chơi, không cần rút ngắn giả tạo.\n" +
			$"State hiện tại: relX={relX:F2}, relY={relY:F2}, " +
			$"playerVelX={playerVelX:F1}, playerVelY={playerVelY:F1}, " +
			$"dist={dist:F2}, hpPercent={hpPercent:F1}, isPatrol={isPatrol}, isChase={isChase}\n" +
			"Gợi ý: relX dương nghĩa là người chơi ở phía Đông (d), âm là phía Tây (a).\n" +
			"relY dương nghĩa là người chơi ở phía Nam (s), âm là phía Bắc (w).\n" +
			"Có thể chia nhỏ thành nhiều đoạn xen kẽ hướng để né vật cản dự đoán được, ví dụ:\n" +
			"'w 5.0 a 10.0 w 3.0 d 4.0 w 3.0'";
	}

	private static string ExtractText(string json)
	{
		using var doc = JsonDocument.Parse(json);
		return doc.RootElement
			.GetProperty("choices")[0]
			.GetProperty("message")
			.GetProperty("content")
			.GetString() ?? "";
	}
}
