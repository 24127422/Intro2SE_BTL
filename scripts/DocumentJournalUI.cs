using Godot;
using System.Collections.Generic;

public partial class DocumentJournalUI : Control
{
	[ExportGroup("UI References - Left List")]
	[Export] public LineEdit SearchBar { get; set; }
	[Export] public VBoxContainer DocumentListContainer { get; set; }
	[Export] public PackedScene DocumentButtonPrefab { get; set; } // Prefab cho từng nút bấm tài liệu
	[Export] public Label UnreadBadgeLabel { get; set; }

	[ExportGroup("UI References - Right Reader")]
	[Export] public Control ReaderPanel { get; set; }
	[Export] public Label TitleLabel { get; set; }
	[Export] public Label MetaLabel { get; set; } // Tác giả & Ngày
	[Export] public TextureRect DocumentImage { get; set; }
	[Export] public RichTextLabel ContentLabel { get; set; }
	[Export] public Label PageNumberLabel { get; set; }
	[Export] public Button PrevPageBtn { get; set; }
	[Export] public Button NextPageBtn { get; set; }
	[Export] public AudioStreamPlayer AudioPlayer { get; set; }

	private Item _selectedDocument;
	private int _currentPage = 0;

	public override void _Ready()
	{
		if (DocumentJournal.Instance != null)
		{
			DocumentJournal.Instance.JournalChanged += RefreshUI;
		}

		if (SearchBar != null) SearchBar.TextChanged += _ => RefreshUI();
		if (PrevPageBtn != null) PrevPageBtn.Pressed += OnPrevPagePressed;
		if (NextPageBtn != null) NextPageBtn.Pressed += OnNextPagePressed;

		ReaderPanel?.Hide();
		RefreshUI();
	}

	public void ToggleJournal()
	{
		Visible = !Visible;
		if (Visible)
		{
			RefreshUI();
		}
	}

	private void RefreshUI()
	{
		if (DocumentJournal.Instance == null) return;

		// 1. Cập nhật Badge số thư chưa đọc
		int unread = DocumentJournal.Instance.GetUnreadCount();
		if (UnreadBadgeLabel != null)
		{
			UnreadBadgeLabel.Text = unread > 0 ? $"{unread} NEW" : "";
			UnreadBadgeLabel.Visible = unread > 0;
		}

		// 2. Xóa danh sách cũ
		foreach (Node child in DocumentListContainer.GetChildren())
		{
			child.QueueFree();
		}

		// 3. Nạp danh sách đã mở khóa
		string query = SearchBar?.Text ?? "";
		List<Item> docs = DocumentJournal.Instance.GetUnlockedDocuments(query);

		foreach (Item doc in docs)
		{
			Button btn = CreateDocumentButton(doc);
			DocumentListContainer.AddChild(btn);
		}

		// Refresh reader nếu tài liệu đang chọn bị bỏ chọn
		if (_selectedDocument != null && !docs.Contains(_selectedDocument))
		{
			ReaderPanel?.Hide();
			_selectedDocument = null;
		}
	}

	private Button CreateDocumentButton(Item doc)
	{
		Button btn = DocumentButtonPrefab != null 
			? DocumentButtonPrefab.Instantiate<Button>() 
			: new Button();

		bool isRead = DocumentJournal.Instance.IsRead(doc);
		string prefix = isRead ? "  " : "🔴 "; // Đánh dấu chưa đọc
		btn.Text = $"{prefix}{doc.ItemName}";
		btn.Icon = doc.Icon;
		btn.Alignment = HorizontalAlignment.Left;

		btn.Pressed += () => SelectDocument(doc);
		return btn;
	}

	private void SelectDocument(Item doc)
	{
		_selectedDocument = doc;
		_currentPage = 0;

		DocumentJournal.Instance.MarkAsRead(doc);
		ReaderPanel?.Show();

		// Phát âm thanh nếu có
		if (doc.PageTurnSound != null && AudioPlayer != null)
		{
			AudioPlayer.Stream = doc.PageTurnSound;
			AudioPlayer.Play();
		}

		DisplayCurrentPage();
	}

	private void DisplayCurrentPage()
	{
		if (_selectedDocument == null) return;

		TitleLabel.Text = _selectedDocument.ItemName;
		MetaLabel.Text = $"Author: {_selectedDocument.Author}  |  Date: {_selectedDocument.Date}";

		// Hiển thị hình ảnh nếu có
		if (DocumentImage != null)
		{
			DocumentImage.Texture = _selectedDocument.DocumentImage;
			DocumentImage.Visible = _selectedDocument.DocumentImage != null;
		}

		// Trang văn bản
		var pages = _selectedDocument.Pages;
		if (pages != null && pages.Count > 0)
		{
			_currentPage = Mathf.Clamp(_currentPage, 0, pages.Count - 1);
			ContentLabel.Text = pages[_currentPage];
			PageNumberLabel.Text = $"{_currentPage + 1} / {pages.Count}";

			PrevPageBtn.Disabled = (_currentPage == 0);
			NextPageBtn.Disabled = (_currentPage >= pages.Count - 1);
		}
		else
		{
			// Fallback dùng Description nếu không có Pages
			ContentLabel.Text = _selectedDocument.Description;
			PageNumberLabel.Text = "1 / 1";
			PrevPageBtn.Disabled = true;
			NextPageBtn.Disabled = true;
		}
	}

	private void OnPrevPagePressed()
	{
		if (_currentPage > 0)
		{
			_currentPage--;
			DisplayCurrentPage();
		}
	}

	private void OnNextPagePressed()
	{
		if (_selectedDocument != null && _currentPage < _selectedDocument.Pages.Count - 1)
		{
			_currentPage++;
			DisplayCurrentPage();
		}
	}
}