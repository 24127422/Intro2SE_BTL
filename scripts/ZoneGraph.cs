using System;
using System.Collections.Generic;

public static class ZoneGraph
{
	public static readonly Dictionary<string, string[]> Adjacency = new()
	{
		["hallway6_n"] = new[] { "hallway6_s", "room63", "room64", "room65" },
		["hallway6_s"] = new[] { "hallway6_n", "room60", "room61", "room62", "toilet6", "hallway5_s" },
		["room60"]     = new[] { "hallway6_s" },
		["room61"]     = new[] { "hallway6_s" },
		["room62"]     = new[] { "hallway6_s" },
		["room63"]     = new[] { "hallway6_n" },
		["room64"]     = new[] { "hallway6_n" },
		["room65"]     = new[] { "hallway6_n" },
		["toilet6"]    = new[] { "hallway6_s" },
		["hallway5_n"] = new[] { "hallway5_s", "room53", "room54", "room55" },
		["hallway5_s"] = new[] { "hallway6_s", "hallway5_n", "room50", "room51", "room52", "toilet5" },
		["room50"]     = new[] { "hallway5_s" },
		["room51"]     = new[] { "hallway5_s" },
		["room52"]     = new[] { "hallway5_s" },
		["room53"]     = new[] { "hallway5_n" },
		["room54"]     = new[] { "hallway5_n" },
		["room55"]     = new[] { "hallway5_n" },
		["toilet5"]    = new[] { "hallway5_s" },
	};

	public static bool IsAdjacent(string a, string b)
		=> Adjacency.TryGetValue(a, out var n) && Array.IndexOf(n, b) >= 0;
		
	public static string GetTransitionName(string fromZone, string toZone)
	{
		bool fromHallway = fromZone.StartsWith("hallway");
		bool toHallway = toZone.StartsWith("hallway");

		if (fromHallway && toHallway)
		{
			string fromFloor = GetHallwayFloor(fromZone);
			string toFloor = GetHallwayFloor(toZone);

			if (fromFloor == toFloor)
				return null; // vd hallway6_n <-> hallway6_s: đi bộ trực tiếp, không cần cửa

			if (fromZone == "hallway6_s" && toZone == "hallway5_s") return "stair_6_down";
			if (fromZone == "hallway5_s" && toZone == "hallway6_s") return "stair_5_up";

			return null; // cặp không hợp lệ theo graph hiện tại
		}

		// Một bên là hallway, bên kia là room/toilet
		string roomZone = fromHallway ? toZone : fromZone;
		string suffix = roomZone.StartsWith("room") ? roomZone.Substring(4) : roomZone;

		bool entering = fromHallway; // đi từ hallway VÀO room -> dùng cửa ngoài (door_x)
		return entering ? $"door_{suffix}" : $"door_{suffix}_in";
	}

	private static string GetHallwayFloor(string zone)
		=> zone.Length > 7 ? zone.Substring(7, 1) : ""; // "hallway6_n" -> "6"
}
