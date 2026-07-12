using Godot;

// Một dòng thoại: ai nói, nói gì, và (tùy chọn) thưởng item khi dòng này hiện ra
[GlobalClass]
public partial class DialogueLine : Resource
{
	[Export] public string SpeakerName { get; set; } = "";

	[Export(PropertyHint.MultilineText)]
	public string Text { get; set; } = "";

	// -1 = kết thúc hội thoại ngay tại dòng này
	[Export] public int NextLineIndex { get; set; } = -1;

	// Nếu mảng này có phần tử, DialogueUI sẽ hiện nút bấm thay vì tự động chuyển dòng bằng phím E
	[Export] public Godot.Collections.Array<DialogueChoice> Choices { get; set; } = new();

	// THƯỞNG: item được cộng vào túi đồ ngay khi dòng thoại này hiển thị lần đầu.
	// Để trống (null) nếu dòng này không thưởng gì.
	[Export] public Item RewardItem { get; set; } = null;
	[Export] public int RewardQuantity { get; set; } = 1;
}
