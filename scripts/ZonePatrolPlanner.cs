using System;
using System.Collections.Generic;
using System.Linq;

public static class ZonePatrolPlanner
{
	public static List<string> GenerateRoute(
		string startZone, Dictionary<string, float> zoneStats, int routeLength, Random rng)
	{
		var route = new List<string> { startZone };
		string current = startZone, previous = null;

		for (int i = 0; i < routeLength; i++)
		{
			if (!ZoneGraph.Adjacency.TryGetValue(current, out var neighbors) || neighbors.Length == 0)
				break;

			var candidates = neighbors.Where(n => n != previous).ToArray();
			if (candidates.Length == 0) candidates = neighbors;

			string next = PickWeightedZone(candidates, zoneStats, current, rng);
			route.Add(next);
			previous = current;
			current = next;
		}
		return route;
	}

	private static string PickWeightedZone(
		string[] candidates, Dictionary<string, float> zoneStats, string fromZone, Random rng)
	{
		const float epsilon = 1f;
		var weights = new float[candidates.Length];
		float total = 0f;

		for (int i = 0; i < candidates.Length; i++)
		{
			float ownStat = zoneStats.GetValueOrDefault(candidates[i], 0f);

			float lookahead = 0f;
			if (ZoneGraph.Adjacency.TryGetValue(candidates[i], out var next))
				foreach (var n in next)
					if (n != fromZone) lookahead += zoneStats.GetValueOrDefault(n, 0f);

			float score = ownStat + lookahead * 0.3f + epsilon;
			weights[i] = score * score;
			total += weights[i];
		}

		double roll = rng.NextDouble() * total, cum = 0;
		for (int i = 0; i < candidates.Length; i++)
		{
			cum += weights[i];
			if (roll <= cum) return candidates[i];
		}
		return candidates[^1];
	}
}
