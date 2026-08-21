using Godot;

public partial class GroqConfig : Node
{
	public static GroqConfig Instance { get; private set; }
	public string ApiKey { get; private set; } = "";

	public override void _Ready()
	{
		Instance = this;
		ApiKey = OS.GetEnvironment("GROQ_API_KEY");

		if (string.IsNullOrEmpty(ApiKey))
		{
			string envPath = "res://.env";
			if (FileAccess.FileExists(envPath))
			{
				var f = FileAccess.Open(envPath, FileAccess.ModeFlags.Read);
				while (!f.EofReached())
				{
					string line = f.GetLine().Trim();
					if (line.StartsWith("GROQ_API_KEY="))
					{
						ApiKey = line.Substring("GROQ_API_KEY=".Length).Trim();
						break;
					}
				}
				f.Close();
			}
		}

		if (string.IsNullOrEmpty(ApiKey))
			GD.PrintErr("[GroqConfig] Chưa cấu hình GROQ_API_KEY.");
	}
}
