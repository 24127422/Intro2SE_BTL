using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using HttpClient = System.Net.Http.HttpClient;
using StringContent = System.Net.Http.StringContent;

public static class GroqZoneRouteAI
{
	private static readonly HttpClient _http = new HttpClient
	{
		Timeout = TimeSpan.FromSeconds(6)
	};

	private const string Url = "https://api.groq.com/openai/v1/chat/completions";
	private const string Model = "openai/gpt-oss-120b";

	public static async Task<List<string>> RequestRouteAsync(
		string currentZone, Dictionary<string, float> zoneStats, int desiredLength)
	{
		string apiKey = GroqConfig.Instance?.ApiKey;
		if (string.IsNullOrEmpty(apiKey))
		{
			GD.PrintErr("[GroqZoneRouteAI] Không có API key -> để Enemy tự fallback.");
			return null;
		}
		
		string statsDebug = zoneStats.Count == 0
			? "(rỗng)"
			: string.Join(", ", zoneStats.OrderByDescending(x => x.Value).Select(kv => $"{kv.Key}={kv.Value:F1}s"));
		GD.Print($"[GroqZoneRouteAI] Zone hiện tại: {currentZone} | Thống kê gửi Groq: {statsDebug}");

		string prompt = BuildPrompt(currentZone, zoneStats, desiredLength);

		var payload = new
		{
			model = Model,
			messages = new[] { new { role = "user", content = prompt } },
			temperature = 0.7,
			max_tokens = 600,
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
				GD.PrintErr($"[GroqZoneRouteAI] HTTP {response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
				return null;
			}

			string body = await response.Content.ReadAsStringAsync();
			string raw = ExtractText(body);
			if (string.IsNullOrWhiteSpace(raw))
				GD.PrintErr($"[GroqZoneRouteAI] Content rỗng! Full response: {body}");
			else
				GD.Print($"[GroqZoneRouteAI] Raw route từ Groq: '{raw}'");

			return ParseAndValidateRoute(raw, currentZone);
		}
		catch (Exception ex)
		{
			GD.PrintErr($"[GroqZoneRouteAI] Lỗi gọi API: {ex.Message}");
			return null;
		}
	}

	private static string BuildPrompt(string currentZone, Dictionary<string, float> zoneStats, int desiredLength)
	{
		var sb = new StringBuilder();

		sb.AppendLine("Bạn là AI điều khiển tuần tra (patrol) cho 1 kẻ địch trong game 2D top-down horror.");
		sb.AppendLine("Nhiệm vụ: chọn 1 lộ trình tuần tra qua các ZONE (không phải toạ độ px) sao cho");
		sb.AppendLine("có xác suất cao nhất chạm mặt người chơi, dựa trên thống kê thời gian người chơi từng ở mỗi zone.");
		sb.AppendLine();
		sb.AppendLine("QUY TẮC BẮT BUỘC:");
		sb.AppendLine("- Chỉ được đi qua các zone có LIÊN KẾT TRỰC TIẾP với nhau theo bảng adjacency bên dưới.");
		sb.AppendLine("- Zone đầu tiên trong route PHẢI là zone hiện tại của kẻ địch.");
		sb.AppendLine($"- Route có đúng {desiredLength + 1} zone (kể cả zone hiện tại).");
		sb.AppendLine("- Ưu tiên các zone có thời gian hoạt động của người chơi (giây) càng cao càng tốt,");
		sb.AppendLine("  nhưng vẫn phải là đường đi hợp lệ liên tục qua các cạnh nối nhau.");
		sb.AppendLine("- CHỈ trả lời đúng 1 dòng: các tên zone cách nhau bởi dấu phẩy, không giải thích, không markdown.");
		sb.AppendLine("  Ví dụ: hallway6_n,hallway6_s,hallway5_s,hallway5_n,room55");
		sb.AppendLine();
		sb.AppendLine($"Zone hiện tại của kẻ địch: {currentZone}");
		sb.AppendLine();
		sb.AppendLine("Adjacency (zone: các zone liền kề):");
		foreach (var kv in ZoneGraph.Adjacency)
			sb.AppendLine($"{kv.Key}: {string.Join(", ", kv.Value)}");
		sb.AppendLine();
		sb.AppendLine("Thống kê thời gian người chơi ở mỗi zone (giây), sắp xếp cao -> thấp:");
		foreach (var kv in zoneStats.OrderByDescending(x => x.Value))
			sb.AppendLine($"{kv.Key}: {kv.Value:F1}s");

		return sb.ToString();
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

	private static List<string> ParseAndValidateRoute(string raw, string currentZone)
	{
		if (string.IsNullOrWhiteSpace(raw)) return null;

		var tokens = raw
			.Replace("\n", ",")
			.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
			.Select(t => t.Trim().ToLowerInvariant())
			.Where(t => ZoneGraph.Adjacency.ContainsKey(t))
			.ToList();

		if (tokens.Count == 0) return null;

		if (tokens[0] != currentZone)
			tokens.Insert(0, currentZone);

		var validated = new List<string> { tokens[0] };
		for (int i = 1; i < tokens.Count; i++)
		{
			if (ZoneGraph.IsAdjacent(validated[^1], tokens[i]))
				validated.Add(tokens[i]);
			else
				break;
		}

		return validated.Count >= 2 ? validated : null;
	}
}
