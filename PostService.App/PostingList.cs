using PostService.Dtos;
using System.Net;
namespace PostService.App;
public partial class PostingList : Form
{
    private readonly PostServiceClient client = new();
    private readonly Button add = new() { Text = "Нове відправлення", Bounds = new Rectangle(280, 448, 190, 36), Anchor = AnchorStyles.Bottom | AnchorStyles.Left };
    private readonly Button edit = new() { Text = "Редагувати", Bounds = new Rectangle(484, 448, 150, 36), Anchor = AnchorStyles.Bottom | AnchorStyles.Left };

    public PostingList()
    {
        InitializeComponent();
        Controls.Add(add);
        Controls.Add(edit);
        add.Click += async (s, e) =>
        {
            using var dialog = new PostingEdit(client);
            if (dialog.ShowDialog(this) == DialogResult.OK) await LoadPostingsAsync();
        };
        edit.Click += async (s, e) =>
        {
            if (gridPostings.CurrentRow?.DataBoundItem is not PostingGetDto posting) return;
            using var dialog = new PostingEdit(client, posting.Id);
            if (dialog.ShowDialog(this) == DialogResult.OK) await LoadPostingsAsync();
        };
    }

    private void SetBusy(bool busy)
    {
        btnRefresh.Enabled = btnDelete.Enabled = add.Enabled = edit.Enabled = gridPostings.Enabled = !busy;
    }

    private async Task LoadPostingsAsync()
    {
        SetBusy(true);
        try
        {
            await RequestRetry.RunAsync(async () => gridPostings.DataSource = await client.GetPostingsAsync());
        }
        finally { SetBusy(false); }
    }

    private async void PostingList_Load(object? sender, EventArgs e)
    {
        await LoadPostingsAsync();
    }

    private async void btnRefresh_Click(object? sender, EventArgs e)
    {
        await LoadPostingsAsync();
    }

    private async void btnDelete_Click(object? sender, EventArgs e)
    {
        if (gridPostings.CurrentRow?.DataBoundItem is not PostingGetDto posting) return;
        if (MessageBox.Show("Видалити відправлення?", "Підтвердження", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
        SetBusy(true);
        bool refresh = false;
        try
        {
            refresh = await RequestRetry.RunAsync(() => client.DeletePostingAsync(posting.Id));
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            MessageBox.Show("Відправлення вже відсутнє. Список буде оновлено.");
            refresh = true;
        }
        finally { SetBusy(false); }
        if (refresh) await LoadPostingsAsync();
    }
}
