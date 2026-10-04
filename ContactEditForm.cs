using System.Text.RegularExpressions;

namespace Phonebook;

internal sealed class ContactEditForm : Form
{
    private readonly TextBox _nameTextBox = new();
    private readonly TextBox _phoneTextBox = new();
    private readonly TextBox _emailTextBox = new();
    private readonly ComboBox _groupComboBox = new();
    private readonly TextBox _notesTextBox = new();
    private readonly DateTimePicker _birthdayPicker = new();
    private bool _formattingPhone;

    public Contact Contact { get; private set; }

    public ContactEditForm(IEnumerable<string> groups, Contact? contact = null)
    {
        Contact = contact is null
            ? new Contact()
            : new Contact
            {
                Id = contact.Id,
                Name = contact.Name,
                Phone = contact.Phone,
                Email = contact.Email,
                Notes = contact.Notes,
                DateOfBirth = contact.DateOfBirth
            };

        InitializeForm(contact is null ? "Новый контакт" : "Редактирование контакта", groups);
        if (contact is not null)
            LoadContact(contact);
    }

    private void InitializeForm(string title, IEnumerable<string> groups)
    {
        Text = title;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(440, 430);
        BackColor = Color.FromArgb(240, 245, 250);
        Font = new Font("Segoe UI", 10F);

        var fields = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 6,
            Padding = new Padding(18),
            BackColor = BackColor
        };
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 125));
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        fields.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

        AddField(fields, "Имя", _nameTextBox, 0);
        AddField(fields, "Телефон", _phoneTextBox, 1);
        AddField(fields, "E-mail", _emailTextBox, 2);
        _groupComboBox.DropDownStyle = ComboBoxStyle.DropDown;
        _groupComboBox.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
        _groupComboBox.AutoCompleteSource = AutoCompleteSource.ListItems;
        _groupComboBox.Items.AddRange(groups.Cast<object>().ToArray());
        AddField(fields, "Группа", _groupComboBox, 3);
        _notesTextBox.Multiline = true;
        _notesTextBox.ScrollBars = ScrollBars.Vertical;
        AddField(fields, "Заметки", _notesTextBox, 4);
        _birthdayPicker.Format = DateTimePickerFormat.Short;
        AddField(fields, "Дата рождения", _birthdayPicker, 5);

        _phoneTextBox.TextChanged += PhoneTextBox_TextChanged;
        _phoneTextBox.KeyPress += PhoneTextBox_KeyPress;

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 58,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(12, 8, 12, 8),
            BackColor = BackColor
        };
        var cancelButton = new Button { Text = "Отмена", Width = 110, Height = 34, DialogResult = DialogResult.Cancel };
        var saveButton = new Button { Text = "Сохранить", Width = 120, Height = 34 };
        saveButton.Click += SaveButton_Click;
        buttons.Controls.Add(cancelButton);
        buttons.Controls.Add(saveButton);

        Controls.Add(fields);
        Controls.Add(buttons);
        AcceptButton = saveButton;
        CancelButton = cancelButton;
    }

    private static void AddField(TableLayoutPanel panel, string labelText, Control input, int row)
    {
        panel.Controls.Add(new Label
        {
            Text = labelText,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, row);
        input.Dock = DockStyle.Fill;
        if (input is TextBox textBox)
            textBox.Margin = new Padding(3, 7, 3, 7);
        panel.Controls.Add(input, 1, row);
    }

    private void LoadContact(Contact contact)
    {
        _nameTextBox.Text = contact.Name;
        _phoneTextBox.Text = PhoneNumberFormatter.Format(contact.Phone);
        _emailTextBox.Text = contact.Email;
        _groupComboBox.Text = contact.GroupName;
        _notesTextBox.Text = contact.Notes;
        _birthdayPicker.Value = contact.DateOfBirth ?? DateTime.Today;
    }

    private void PhoneTextBox_KeyPress(object? sender, KeyPressEventArgs e)
    {
        if (e.KeyChar == '+' &&
            _phoneTextBox.SelectionStart == 0 &&
            (_phoneTextBox.SelectionLength > 0 || !_phoneTextBox.Text.Contains('+')))
        {
            return;
        }

        if (!char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar))
            e.Handled = true;
    }

    private void PhoneTextBox_TextChanged(object? sender, EventArgs e)
    {
        if (_formattingPhone)
            return;

        var text = _phoneTextBox.Text;
        var digitCountBeforeCaret = text[.._phoneTextBox.SelectionStart].Count(char.IsDigit);
        var formatted = PhoneNumberFormatter.Format(text);
        if (text == formatted)
            return;

        _formattingPhone = true;
        try
        {
            _phoneTextBox.Text = formatted;
            _phoneTextBox.SelectionStart = PhoneNumberFormatter.GetCaretPosition(formatted, digitCountBeforeCaret);
            _phoneTextBox.SelectionLength = 0;
        }
        finally
        {
            _formattingPhone = false;
        }
    }

    private void SaveButton_Click(object? sender, EventArgs e)
    {
        var name = _nameTextBox.Text.Trim();
        var phone = PhoneNumberFormatter.Normalize(_phoneTextBox.Text);
        var email = _emailTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(phone))
        {
            MessageBox.Show(this, "Имя и телефон обязательны для заполнения.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (phone.Length is < 11 or > 13)
        {
            MessageBox.Show(this, "Телефон должен содержать код страны из 1–3 цифр и ещё 10 цифр.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!string.IsNullOrWhiteSpace(email) &&
            !Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
        {
            MessageBox.Show(this, "Введите корректный E-mail или оставьте поле пустым.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        Contact = new Contact
        {
            Id = Contact.Id,
            Name = name,
            Phone = phone,
            Email = email,
            GroupName = _groupComboBox.Text.Trim(),
            Notes = _notesTextBox.Text.Trim(),
            DateOfBirth = _birthdayPicker.Value.Date
        };
        DialogResult = DialogResult.OK;
        Close();
    }
}
