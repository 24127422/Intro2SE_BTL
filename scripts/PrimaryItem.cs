using Godot;

// Kế thừa Item để tái sử dụng toàn bộ field chung (ItemName, Icon, HandModel,
// TextureNorth/South/East/West...) vốn đã dùng cho hệ thống cầm-tay (PlayerHand).
//
// THIẾT KẾ: đây là 1 class DÙNG CHUNG cho MỌI "dụng cụ chính" có thể kích hoạt/bật-tắt
// (đèn pin, bình cứu hỏa, radio dò sóng, chìa khóa điện tử có pin...) — đúng tinh thần
// tái sử dụng mà ItemPickup.cs đang áp dụng cho Item: KHÔNG tạo FlashlightItem,
// FireExtinguisherItem... riêng từng class, mà tạo nhiều Resource .tres khác nhau
// (VD: "Đèn pin.tres", "Bình cứu hỏa.tres") cùng dùng class PrimaryItem này, chỉ khác
// giá trị các [Export] bên dưới. Muốn thêm dụng cụ mới -> tạo .tres mới, không cần viết
// thêm code C#.
[GlobalClass]
public partial class PrimaryItem : Item
{
	[ExportGroup("Primary Item - Kích hoạt")]
	// true  = bấm dùng để BẬT/TẮT lặp lại (đèn pin: bật rồi tắt khi hết pin thì bật lại được)
	// false = giữ để dùng liên tục, buông ra là tắt ngay (bình cứu hỏa: giữ nút xịt)
	[Export] public bool IsToggleable { get; set; } = true;

	// Vừa trang bị vào tay là bật sẵn hay chưa (thường để false)
	[Export] public bool StartsActive { get; set; } = false;

	[ExportGroup("Primary Item - Năng lượng / Độ bền")]
	// Dung lượng tối đa: pin đèn pin, áp suất bình cứu hỏa... <= 0 nghĩa là dùng vô hạn,
	// không hao hụt (vd chìa khóa điện tử dùng nhiều lần không giới hạn).
	[Export] public float MaxDurability { get; set; } = 100f;

	// Tốc độ hao hụt mỗi giây trong lúc đang Active (đèn pin đang sáng, bình cứu hỏa đang xịt).
	[Export] public float DrainPerSecond { get; set; } = 5f;

	// Hết năng lượng thì có tự động tắt không (mặc định có)
	[Export] public bool AutoDeactivateWhenEmpty { get; set; } = true;

	[ExportGroup("Primary Item - Hiệu ứng khi kích hoạt")]
	// Scene hiệu ứng gắn thêm vào tay khi Active: vùng sáng đèn pin (Light2D),
	// tia bọt/particle của bình cứu hỏa... Để trống nếu dụng cụ không cần hiệu ứng riêng.
	[Export] public PackedScene ActiveEffectScene { get; set; }

	[ExportGroup("Primary Item - Âm thanh")]
	[Export] public AudioStream ActivateSound { get; set; }
	[Export] public AudioStream DeactivateSound { get; set; }
	[Export] public AudioStream DepletedSound { get; set; } // phát khi hết năng lượng giữa chừng
}
