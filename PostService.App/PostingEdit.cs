using PostService.Dtos;
using PostService.CommonTypes;
using System.Net;

namespace PostService.App;

public class PostingEdit : Form
{
    private readonly IPostServiceClient client;
    private readonly int id;
    private readonly TextBox from = new TextBox();
    private readonly TextBox to = new TextBox();
    private readonly TextBox content = new TextBox();
    private readonly ComboBox delivery = new ComboBox();
    private readonly NumericUpDown[] numbers = new NumericUpDown[6];

    private readonly Button save = new Button();
    private readonly Button cancel = new Button();

    public PostingEdit(IPostServiceClient client, int id = 0)
    {
        this.client = client;
        this.id = id;
        Text = id == 0 ? "Нове відправлення" : $"Редагування № {id}";
        ClientSize = new Size(850, 650);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        Font = new Font("Segoe UI", 10);

        var title = new Label { Text = Text, Font = new Font(Font.FontFamily, 18, FontStyle.Bold), Bounds = new Rectangle(24, 16, 790, 46) };
        Controls.Add(title);
        delivery.DropDownStyle = ComboBoxStyle.DropDownList;
        delivery.Items.AddRange(new object[] { "У відділення", "Кур’єр", "Експрес-кур’єр" });
        delivery.SelectedIndex = 0;
        string[] names = { "Відправник", "Отримувач", "Вміст", "Тип доставки", "Вага, кг", "Ширина, см", "Висота, см", "Глибина, см", "Оголошена цінність, грн", "Вартість, грн" };
        for (int i = 0; i < 6; i++)
        {
            numbers[i] = new NumericUpDown { Minimum = 0, Maximum = 1000000, DecimalPlaces = i == 0 || i > 3 ? 2 : 0 };
            numbers[i].Value = i < 4 ? 1 : 0;
        }
        Control[] fields = { from, to, content, delivery, numbers[0], numbers[1], numbers[2], numbers[3], numbers[4], numbers[5] };
        for (int i = 0; i < fields.Length; i++)
        {
            Controls.Add(new Label { Text = names[i], Bounds = new Rectangle(24, 84 + i * 44, 240, 30) });
            fields[i].Bounds = new Rectangle(275, 80 + i * 44, 510, 30);
            Controls.Add(fields[i]);
        }
        numbers[5].Maximum = 30000000;
        numbers[5].ReadOnly = id == 0;
        numbers[5].Enabled = id != 0;
        numbers[0].ValueChanged += Calculate;
        delivery.SelectedIndexChanged += Calculate;
        Calculate(null, EventArgs.Empty);



        save.Text = "Зберегти";
        save.Bounds = new Rectangle(275, 590, 160, 40);
        save.BackColor = Color.FromArgb(34, 103, 175);
        save.ForeColor = Color.White;
        save.Click += Save;
        cancel.Text = "Скасувати";
        cancel.Bounds = new Rectangle(452, 590, 160, 40);
        cancel.DialogResult = DialogResult.Cancel;
        Controls.Add(save);
        Controls.Add(cancel);
        AcceptButton = save;
        CancelButton = cancel;
        FormClosing += (s, e) => { if (!save.Enabled) e.Cancel = true; };
        Shown += LoadData;
    }

    private void Calculate(object? sender, EventArgs e)
    {
        if (id == 0)
            numbers[5].Value = Math.Min(numbers[5].Maximum, numbers[0].Value * (delivery.SelectedIndex + 1) * 10);
    }

    private async void LoadData(object? sender, EventArgs e)
    {
        if (id == 0) return;
        save.Enabled = false;
        Enabled = false;
        try
        {
            PostingGetDto p = null!;
            if (!await RequestRetry.RunAsync(async () => p = await client.GetPostingByIdAsync(id)))
            {
                save.Enabled = true;
                DialogResult = DialogResult.Cancel;
                return;
            }
            from.Text = p.From;
            to.Text = p.To;
            content.Text = p.Content;
            delivery.SelectedIndex = (int)p.DeliveryType;
            decimal[] values = { (decimal)p.Weight, p.Width, p.Height, p.Depth, (decimal)p.Value, (decimal)p.Price };
            for (int i = 0; i < 6; i++) numbers[i].Value = Math.Clamp(values[i], 0, numbers[i].Maximum);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            MessageBox.Show("Відправлення вже відсутнє. Список буде оновлено.");
            save.Enabled = true;
            DialogResult = DialogResult.OK;
        }
        finally { save.Enabled = true; Enabled = true; }
    }

    private async void Save(object? sender, EventArgs e)
    {


        save.Enabled = false;
        cancel.Enabled = false;
        bool done = false;
        try
        {
            var data = new PostingPutDto
            {
                Id = id, From = from.Text.Trim(), To = to.Text.Trim(), Content = content.Text.Trim(),
                DeliveryType = (DeliveryType)delivery.SelectedIndex, Weight = (float)numbers[0].Value,
                Width = (int)numbers[1].Value, Height = (int)numbers[2].Value, Depth = (int)numbers[3].Value,
                Value = (float)numbers[4].Value, Price = (float)numbers[5].Value
            };
            done = await RequestRetry.RunAsync(async () =>
            {
                if (id == 0)
                    await client.CreatePostingAsync(new PostingPostDto
                    {
                        From = data.From, To = data.To, Content = data.Content,
                        DeliveryType = data.DeliveryType, Weight = data.Weight,
                        Width = data.Width, Height = data.Height, Depth = data.Depth, Value = data.Value
                    });
                else
                    await client.UpdatePostingAsync(id, data);
            });
        }
        catch (PostingValidationException ex)
        {
            MessageBox.Show(string.Join(Environment.NewLine, ex.Errors.Values.SelectMany(x => x)), "Перевірте введені дані");
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound && id != 0)
        {
            MessageBox.Show("Відправлення вже відсутнє. Список буде оновлено.");
            done = true;
        }
        finally { save.Enabled = true; cancel.Enabled = true; }
        if (done) DialogResult = DialogResult.OK;
    }

}

