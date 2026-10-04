using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.Text;

namespace Phonebook;

public partial class Form1 : Form
{
    private readonly PhonebookDatabase _database = new();
    private readonly PrintDocument _printDocument = new();
    private List<Contact> _printContacts = new();
    private readonly HashSet<int> _checkedContactIds = new();
    private readonly Dictionary<string, bool> _sortDirections = new(StringComparer.Ordinal);
    private bool _updatingCheckboxes;

    public Form1()
    {
        InitializeComponent();
        _database.Initialize();
        _printDocument.PrintPage += PrintDocument_PrintPage;
        LoadContacts();
    }

    private static void ApplyButtonTheme(Button button, Color baseColor, Color hoverColor, Color pressedColor)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.BackColor = baseColor;
        button.ForeColor = Color.White;
        button.UseVisualStyleBackColor = false;
        button.Cursor = Cursors.Hand;
        button.TextAlign = ContentAlignment.MiddleCenter;
        button.Padding = new Padding(6, 2, 6, 2);
        button.Margin = new Padding(0, 0, 0, 10);
        button.Font = new Font("Segoe UI", 10F, FontStyle.Bold);

        var originalColor = baseColor;
        button.MouseEnter += (_, _) =>
        {
            button.BackColor = hoverColor;
            button.Refresh();
        };
        button.MouseLeave += (_, _) =>
        {
            button.BackColor = originalColor;
            button.Refresh();
        };
        button.MouseDown += (_, _) =>
        {
            button.BackColor = pressedColor;
            button.Refresh();
        };
        button.MouseUp += (_, _) =>
        {
            button.BackColor = hoverColor;
            button.Refresh();
        };

        button.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            var bounds = new Rectangle(1, 1, button.Width - 2, button.Height - 2);
            using var path = CreateRoundedRectangle(bounds, 12);
            using var fill = new LinearGradientBrush(bounds, button.BackColor, AdjustColor(button.BackColor, -18), LinearGradientMode.Vertical);
            e.Graphics.FillPath(fill, path);

            using var borderPen = new Pen(AdjustColor(button.BackColor, -35), 1.5f);
            e.Graphics.DrawPath(borderPen, path);

            var textRect = new Rectangle(0, 0, button.Width, button.Height);
            TextRenderer.DrawText(e.Graphics, button.Text, button.Font, textRect, button.ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
        };
    }

    private static GraphicsPath CreateRoundedRectangle(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        var diameter = radius * 2;

        path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
        path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
        path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();

        return path;
    }

    private static Color AdjustColor(Color color, int delta)
    {
        var r = Math.Clamp(color.R + delta, 0, 255);
        var g = Math.Clamp(color.G + delta, 0, 255);
        var b = Math.Clamp(color.B + delta, 0, 255);
        return Color.FromArgb(r, g, b);
    }

    private void LoadContacts(string searchText = "")
    {
        var contacts = _database.GetAll(searchText);
        foreach (var contact in contacts)
        {
            contact.Phone = PhoneNumberFormatter.Format(contact.Phone);
        }

        dgvContacts.DataSource = null;
        dgvContacts.DataSource = contacts;

        if (!dgvContacts.Columns.Contains("SelectForOutput"))
        {
            dgvContacts.Columns.Insert(0, new DataGridViewCheckBoxColumn
            {
                Name = "SelectForOutput",
                HeaderText = string.Empty,
                Width = 32,
                MinimumWidth = 32,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                ReadOnly = false,
                SortMode = DataGridViewColumnSortMode.NotSortable,
                TrueValue = true,
                FalseValue = false
            });
        }

        var selectColumn = dgvContacts.Columns["SelectForOutput"];
        if (selectColumn is not null)
        {
            selectColumn.DisplayIndex = 0;
            selectColumn.ReadOnly = false;
        }

        foreach (DataGridViewRow row in dgvContacts.Rows)
        {
            var id = Convert.ToInt32(row.Cells["Id"].Value);
            row.Cells["SelectForOutput"].Value = _checkedContactIds.Contains(id);
        }

        foreach (DataGridViewColumn column in dgvContacts.Columns)
        {
            column.ReadOnly = column.Name != "SelectForOutput";
        }

        UpdateSelectAllButtonText();

        if (dgvContacts.Columns.Count > 0)
        {
            var idColumn = dgvContacts.Columns["Id"];
            var nameColumn = dgvContacts.Columns["Name"];
            var phoneColumn = dgvContacts.Columns["Phone"];
            var emailColumn = dgvContacts.Columns["Email"];
            var groupColumn = dgvContacts.Columns["GroupName"];
            var notesColumn = dgvContacts.Columns["Notes"];
            var birthdayColumn = dgvContacts.Columns["DateOfBirth"];

            if (idColumn is not null)
                idColumn.Visible = false;

            if (nameColumn is not null)
                nameColumn.HeaderText = "Имя";

            if (phoneColumn is not null)
                phoneColumn.HeaderText = "Телефон";

            if (emailColumn is not null)
                emailColumn.HeaderText = "E-mail";

            if (groupColumn is not null)
                groupColumn.HeaderText = "Группа";

            if (notesColumn is not null)
                notesColumn.HeaderText = "Заметки";

            if (birthdayColumn is not null)
            {
                birthdayColumn.HeaderText = "Дата рождения";
                birthdayColumn.DefaultCellStyle.Format = "dd.MM.yyyy";
            }
        }
    }

    private void btnAdd_Click(object sender, EventArgs e)
    {
        using var editor = new ContactEditForm(_database.GetGroups());
        if (editor.ShowDialog(this) != DialogResult.OK)
            return;

        _database.AddContact(editor.Contact);
        MessageBox.Show("Контакт добавлен.", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
        LoadContacts(txtSearch.Text.Trim());
    }

    private void btnDelete_Click(object sender, EventArgs e)
    {
        var selectedContacts = GetCheckedContacts();
        if (selectedContacts.Count == 0)
        {
            MessageBox.Show("Выберите записи галочками для удаления.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var names = selectedContacts
            .Select(c => c.Name)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Distinct(StringComparer.Ordinal)
            .Take(3)
            .ToList();

        var summary = selectedContacts.Count == 1
            ? $"Удалить запись '{names.FirstOrDefault() ?? "контакт"}'?"
            : $"Удалить {selectedContacts.Count} записей ({string.Join(", ", names)}{(selectedContacts.Count > names.Count ? ", ..." : string.Empty)})?";

        var result = MessageBox.Show(summary, "Подтверждение удаления", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (result != DialogResult.Yes)
            return;

        foreach (var contact in selectedContacts)
        {
            _database.DeleteContact(contact.Id);
            _checkedContactIds.Remove(contact.Id);
        }

        LoadContacts(txtSearch.Text.Trim());
    }

    private void btnEdit_Click(object sender, EventArgs e)
    {
        if (dgvContacts.SelectedRows.Count == 0)
        {
            MessageBox.Show("Выберите запись для редактирования.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var row = dgvContacts.SelectedRows[0];
        var contact = new Contact
        {
            Id = Convert.ToInt32(row.Cells["Id"].Value),
            Name = row.Cells["Name"].Value?.ToString() ?? string.Empty,
            Phone = PhoneNumberFormatter.Normalize(row.Cells["Phone"].Value?.ToString() ?? string.Empty),
            Email = row.Cells["Email"].Value?.ToString() ?? string.Empty,
            GroupName = row.Cells["GroupName"].Value?.ToString() ?? string.Empty,
            Notes = row.Cells["Notes"].Value?.ToString() ?? string.Empty,
            DateOfBirth = row.Cells["DateOfBirth"].Value is DateTime dt ? dt : null
        };

        using var editor = new ContactEditForm(_database.GetGroups(), contact);
        if (editor.ShowDialog(this) != DialogResult.OK)
            return;

        _database.UpdateContact(editor.Contact);
        MessageBox.Show("Контакт обновлён.", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
        LoadContacts(txtSearch.Text.Trim());
    }

    private List<Contact> GetCheckedContacts()
    {
        dgvContacts.EndEdit();
        var checkedRows = dgvContacts.Rows
            .Cast<DataGridViewRow>()
            .Where(row => _checkedContactIds.Contains(Convert.ToInt32(row.Cells["Id"].Value)))
            .ToList();

        return checkedRows
            .Select(row => new Contact
            {
                Id = Convert.ToInt32(row.Cells["Id"].Value),
                Name = row.Cells["Name"].Value?.ToString() ?? string.Empty,
                Phone = row.Cells["Phone"].Value?.ToString() ?? string.Empty,
                Email = row.Cells["Email"].Value?.ToString() ?? string.Empty,
                GroupName = row.Cells["GroupName"].Value?.ToString() ?? string.Empty,
                Notes = row.Cells["Notes"].Value?.ToString() ?? string.Empty,
                DateOfBirth = row.Cells["DateOfBirth"].Value is DateTime dt ? dt : null
            })
            .ToList();
    }

    private void btnExport_Click(object sender, EventArgs e)
    {
        ExportSelectedContacts("csv");
    }

    private void btnExportVcf_Click(object sender, EventArgs e)
    {
        ExportSelectedContacts("vcf");
    }

    private void btnImportVcf_Click(object sender, EventArgs e)
    {
        using var openFileDialog = new OpenFileDialog
        {
            Filter = "VCF files (*.vcf)|*.vcf|All files (*.*)|*.*",
            DefaultExt = "vcf",
            FileName = "contacts.vcf"
        };

        if (openFileDialog.ShowDialog(this) != DialogResult.OK)
            return;

        var importResult = ImportContactsFromVcf(openFileDialog.FileName);
        if (importResult.AddedCount == 0 && importResult.DuplicateCount == 0)
        {
            MessageBox.Show("Файл не содержит корректных контактов для импорта.", "Импорт VCF", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        MessageBox.Show(
            $"Добавлено новых контактов: {importResult.AddedCount}.\nСовпадающих записей объединено: {importResult.DuplicateCount}.",
            "Импорт VCF",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
        LoadContacts(txtSearch.Text.Trim());
    }

    private void btnImportCsv_Click(object sender, EventArgs e)
    {
        using var openFileDialog = new OpenFileDialog
        {
            Filter = "CSV files (*.csv)|*.csv|Text files (*.txt)|*.txt|All files (*.*)|*.*",
            DefaultExt = "csv",
            FileName = "contacts.csv"
        };

        if (openFileDialog.ShowDialog(this) != DialogResult.OK)
            return;

        (int AddedCount, int DuplicateCount, int SkippedCount) importResult;
        try
        {
            importResult = ImportContactsFromCsv(openFileDialog.FileName);
        }
        catch (IOException ex)
        {
            MessageBox.Show($"Не удалось прочитать CSV-файл: {ex.Message}", "Импорт CSV", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }
        catch (UnauthorizedAccessException ex)
        {
            MessageBox.Show($"Нет доступа к CSV-файлу: {ex.Message}", "Импорт CSV", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }
        catch (FormatException ex)
        {
            MessageBox.Show($"Некорректный формат CSV: {ex.Message}", "Импорт CSV", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (importResult.AddedCount == 0 && importResult.DuplicateCount == 0)
        {
            MessageBox.Show(
                $"Файл не содержит корректных контактов для импорта. Пропущено строк: {importResult.SkippedCount}.",
                "Импорт CSV",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        MessageBox.Show(
            $"Добавлено новых контактов: {importResult.AddedCount}.\nСовпадающих записей объединено: {importResult.DuplicateCount}.\nПропущено некорректных строк: {importResult.SkippedCount}.",
            "Импорт CSV",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
        LoadContacts(txtSearch.Text.Trim());
    }

    private (int AddedCount, int DuplicateCount, int SkippedCount) ImportContactsFromCsv(string filePath)
    {
        var text = File.ReadAllText(filePath, Encoding.UTF8);
        var delimiter = DetectCsvDelimiter(text);
        var rows = ParseCsv(text, delimiter);
        if (rows.Count == 0)
            return (0, 0, 0);

        var firstRow = rows[0];
        var hasHeader = TryMapCsvHeaders(firstRow, out var columns);
        var firstDataRow = hasHeader ? 1 : 0;
        if (!hasHeader)
            columns = new Dictionary<string, int>(StringComparer.Ordinal)
            {
                ["name"] = 0,
                ["phone"] = 1,
                ["email"] = 2,
                ["group"] = 3,
                ["notes"] = 4,
                ["birthday"] = 5
            };

        var addedCount = 0;
        var duplicateCount = 0;
        var skippedCount = 0;
        for (var rowIndex = firstDataRow; rowIndex < rows.Count; rowIndex++)
        {
            var row = rows[rowIndex];
            if (row.All(string.IsNullOrWhiteSpace))
                continue;

            var contact = new Contact
            {
                Name = GetCsvField(row, columns, "name"),
                Phone = PhoneNumberFormatter.Normalize(GetCsvField(row, columns, "phone")),
                Email = GetCsvField(row, columns, "email"),
                GroupName = GetCsvField(row, columns, "group"),
                Notes = GetCsvField(row, columns, "notes")
            };

            var birthday = GetCsvField(row, columns, "birthday");
            if (!string.IsNullOrWhiteSpace(birthday)
                && DateTime.TryParseExact(
                    birthday,
                    new[] { "dd.MM.yyyy", "yyyy-MM-dd", "yyyyMMdd", "M/d/yyyy", "MM/dd/yyyy" },
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None,
                    out var parsedBirthday))
            {
                contact.DateOfBirth = parsedBirthday;
            }

            if (string.IsNullOrWhiteSpace(contact.Name) || string.IsNullOrWhiteSpace(contact.Phone))
            {
                skippedCount++;
                continue;
            }

            if (_database.MergeDuplicateContact(contact))
                addedCount++;
            else
                duplicateCount++;
        }

        return (addedCount, duplicateCount, skippedCount);
    }

    private static char DetectCsvDelimiter(string text)
    {
        var semicolons = 0;
        var commas = 0;
        var inQuotes = false;
        for (var i = 0; i < text.Length; i++)
        {
            var ch = text[i];
            if (ch == '"')
            {
                if (inQuotes && i + 1 < text.Length && text[i + 1] == '"')
                {
                    i++;
                    continue;
                }

                inQuotes = !inQuotes;
            }
            else if (!inQuotes && (ch == '\r' || ch == '\n'))
            {
                break;
            }
            else if (!inQuotes && ch == ';')
            {
                semicolons++;
            }
            else if (!inQuotes && ch == ',')
            {
                commas++;
            }
        }

        return commas > semicolons ? ',' : ';';
    }

    private static List<List<string>> ParseCsv(string text, char delimiter)
    {
        var rows = new List<List<string>>();
        var row = new List<string>();
        var field = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < text.Length; i++)
        {
            var ch = text[i];
            if (inQuotes)
            {
                if (ch == '"' && i + 1 < text.Length && text[i + 1] == '"')
                {
                    field.Append('"');
                    i++;
                }
                else if (ch == '"')
                {
                    inQuotes = false;
                }
                else
                {
                    field.Append(ch);
                }

                continue;
            }

            if (ch == '"' && field.Length == 0)
            {
                inQuotes = true;
            }
            else if (ch == delimiter)
            {
                row.Add(field.ToString());
                field.Clear();
            }
            else if (ch == '\r' || ch == '\n')
            {
                row.Add(field.ToString());
                field.Clear();
                if (row.Any(value => value.Length > 0))
                    rows.Add(row);
                row = new List<string>();
                if (ch == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                    i++;
            }
            else
            {
                field.Append(ch);
            }
        }

        if (inQuotes)
            throw new FormatException("CSV содержит незакрытое поле в кавычках.");

        row.Add(field.ToString());
        if (row.Any(value => value.Length > 0))
            rows.Add(row);
        return rows;
    }

    private static bool TryMapCsvHeaders(List<string> headers, out Dictionary<string, int> columns)
    {
        var aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["имя"] = "name",
            ["name"] = "name",
            ["fullname"] = "name",
            ["full name"] = "name",
            ["телефон"] = "phone",
            ["phone"] = "phone",
            ["telephone"] = "phone",
            ["tel"] = "phone",
            ["e-mail"] = "email",
            ["email"] = "email",
            ["электронная почта"] = "email",
            ["группа"] = "group",
            ["group"] = "group",
            ["org"] = "group",
            ["organization"] = "group",
            ["заметки"] = "notes",
            ["notes"] = "notes",
            ["note"] = "notes",
            ["дата рождения"] = "birthday",
            ["birthday"] = "birthday",
            ["dateofbirth"] = "birthday",
            ["date of birth"] = "birthday"
        };

        columns = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < headers.Count; i++)
        {
            var header = headers[i].Trim().TrimStart('\uFEFF');
            if (aliases.TryGetValue(header, out var fieldName))
                columns[fieldName] = i;
        }

        return columns.ContainsKey("name") && columns.ContainsKey("phone");
    }

    private static string GetCsvField(List<string> row, Dictionary<string, int> columns, string fieldName)
    {
        return columns.TryGetValue(fieldName, out var index) && index < row.Count
            ? row[index].Trim()
            : string.Empty;
    }

    private (int AddedCount, int DuplicateCount) ImportContactsFromVcf(string filePath)
    {
        var lines = UnfoldVcfLines(File.ReadAllLines(filePath, Encoding.UTF8));
        var cards = new List<Dictionary<string, List<string>>>();
        var current = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in lines)
        {
            var trimmed = line.TrimEnd();
            if (string.IsNullOrWhiteSpace(trimmed))
                continue;

            if (trimmed.StartsWith("BEGIN:VCARD", StringComparison.OrdinalIgnoreCase))
            {
                current = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
                continue;
            }

            if (trimmed.StartsWith("END:VCARD", StringComparison.OrdinalIgnoreCase))
            {
                if (current.Count > 0)
                    cards.Add(current);
                current = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
                continue;
            }

            var idx = trimmed.IndexOf(':');
            if (idx < 0)
                continue;

            var keyWithParams = trimmed[..idx];
            var value = trimmed[(idx + 1)..];
            var key = keyWithParams.Split(';', 2)[0].Trim();
            if (!current.ContainsKey(key))
                current[key] = new List<string>();

            current[key].Add(DecodeVcfValue(keyWithParams, value));
        }

        if (cards.Count == 0)
            return (0, 0);

        var addedCount = 0;
        var duplicateCount = 0;
        foreach (var card in cards)
        {
            var contact = MapVcfCardToContact(card);
            if (string.IsNullOrWhiteSpace(contact.Name) || string.IsNullOrWhiteSpace(contact.Phone))
                continue;

            if (_database.MergeDuplicateContact(contact))
                addedCount++;
            else
                duplicateCount++;
        }

        return (addedCount, duplicateCount);
    }

    private static List<string> UnfoldVcfLines(string[] rawLines)
    {
        var unfolded = new List<string>();
        var current = new StringBuilder();

        foreach (var rawLine in rawLines)
        {
            var line = rawLine ?? string.Empty;
            if (string.IsNullOrEmpty(line))
            {
                if (current.Length > 0)
                {
                    unfolded.Add(current.ToString());
                    current.Clear();
                }
                continue;
            }

            if (current.Length > 0 && (line.StartsWith(" ") || line.StartsWith("\t")))
            {
                current.Append(line[1..]);
                continue;
            }

            if (current.Length > 0 && current.ToString().EndsWith("=", StringComparison.Ordinal))
            {
                current.Length--;
                current.Append(line);
                continue;
            }

            if (line.EndsWith("=", StringComparison.Ordinal))
            {
                current.Append(line[..^1]);
                continue;
            }

            if (current.Length > 0)
            {
                current.Append(line);
                unfolded.Add(current.ToString());
                current.Clear();
                continue;
            }

            unfolded.Add(line);
        }

        if (current.Length > 0)
            unfolded.Add(current.ToString());

        return unfolded;
    }

    private static Contact MapVcfCardToContact(Dictionary<string, List<string>> card)
    {
        var contact = new Contact();

        var firstNameValue = GetFirstValue(card, "FN");
        var nValue = GetFirstValue(card, "N");
        var fullName = !string.IsNullOrWhiteSpace(firstNameValue) ? firstNameValue : ParseNameFromVcfN(nValue);
        contact.Name = fullName;

        var phoneValue = GetFirstValue(card, "TEL");
        if (string.IsNullOrWhiteSpace(phoneValue))
            phoneValue = GetFirstValue(card, "CELL");
        contact.Phone = PhoneNumberFormatter.Normalize(phoneValue);

        var emailValue = GetFirstValue(card, "EMAIL");
        contact.Email = emailValue;

        var noteValue = GetFirstValue(card, "NOTE");
        contact.Notes = noteValue;

        var orgValue = GetFirstValue(card, "ORG");
        contact.GroupName = orgValue;
        if (string.IsNullOrWhiteSpace(contact.GroupName))
        {
            var separatorIndex = contact.Name.IndexOf('-');
            if (separatorIndex > 0)
                contact.GroupName = contact.Name[..separatorIndex].Trim();
        }

        var birthdayValue = GetFirstValue(card, "BDAY");
        if (!string.IsNullOrWhiteSpace(birthdayValue) && DateTime.TryParseExact(birthdayValue, new[] { "yyyy-MM-dd", "yyyyMMdd", "dd.MM.yyyy" }, null, System.Globalization.DateTimeStyles.None, out var parsedDate))
            contact.DateOfBirth = parsedDate;

        return contact;
    }

    private static string GetFirstValue(Dictionary<string, List<string>> card, string key)
    {
        if (!card.TryGetValue(key, out var values) || values.Count == 0)
            return string.Empty;

        return values[0].Trim();
    }

    private static string ParseNameFromVcfN(string? nValue)
    {
        if (string.IsNullOrWhiteSpace(nValue))
            return string.Empty;

        var parts = nValue.Split(';');
        for (var i = 0; i < parts.Length; i++)
            parts[i] = CleanVcfText(parts[i]);

        var family = parts.Length > 0 ? parts[0] : string.Empty;
        var given = parts.Length > 1 ? parts[1] : string.Empty;
        var additional = parts.Length > 2 ? parts[2] : string.Empty;
        var result = new List<string>();

        if (!string.IsNullOrWhiteSpace(given)) result.Add(given);
        if (!string.IsNullOrWhiteSpace(additional)) result.Add(additional);
        if (!string.IsNullOrWhiteSpace(family)) result.Add(family);

        return string.Join(" ", result);
    }

    private static string DecodeVcfValue(string keyWithParams, string value)
    {
        var key = keyWithParams.Split(';', 2)[0].Trim();
        var normalized = value.Trim();
        var isQuotedPrintable = keyWithParams.Contains("QUOTED-PRINTABLE", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains('=');

        if (isQuotedPrintable)
            normalized = DecodeQuotedPrintable(normalized);

        if (key.Equals("N", StringComparison.OrdinalIgnoreCase) || key.Equals("FN", StringComparison.OrdinalIgnoreCase))
            return CleanVcfText(normalized);

        return CleanVcfText(normalized);
    }

    private static string DecodeQuotedPrintable(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        if (ContainsLegacyUnicodeQuotedPrintable(value))
            return DecodeLegacyUnicodeQuotedPrintable(value);

        var bytes = new List<byte>();
        for (var i = 0; i < value.Length; i++)
        {
            if (value[i] == '=' && i + 2 < value.Length && IsHex(value[i + 1]) && IsHex(value[i + 2]))
            {
                bytes.Add(Convert.ToByte(value.Substring(i + 1, 2), 16));
                i += 2;
                continue;
            }

            if (value[i] == '=' && i + 1 < value.Length && value[i + 1] == '\r' && i + 2 < value.Length && value[i + 2] == '\n')
            {
                i += 2;
                continue;
            }

            if (value[i] == '=' && i + 1 < value.Length && value[i + 1] == '\n')
            {
                i++;
                continue;
            }

            if (value[i] == '=')
                continue;

            if (value[i] <= 0x7F)
            {
                bytes.Add((byte)value[i]);
            }
            else
            {
                bytes.AddRange(Encoding.UTF8.GetBytes(value[i].ToString()));
            }
        }

        if (bytes.Count == 0)
            return string.Empty;

        return Encoding.UTF8.GetString(bytes.ToArray());
    }

    private static bool ContainsLegacyUnicodeQuotedPrintable(string value)
    {
        for (var i = 0; i + 3 < value.Length; i++)
        {
            if (value[i] != '='
                || !IsHex(value[i + 1])
                || !IsHex(value[i + 2])
                || !IsHex(value[i + 3]))
                continue;

            var code = Convert.ToInt32(value.Substring(i + 1, 3), 16);
            if (IsLegacyCyrillicCode(code))
                return true;
        }

        return false;
    }

    private static string DecodeLegacyUnicodeQuotedPrintable(string value)
    {
        var builder = new StringBuilder();
        for (var i = 0; i < value.Length; i++)
        {
            if (value[i] == '=' && i + 2 < value.Length && IsHex(value[i + 1]) && IsHex(value[i + 2]))
            {
                var hexLength = 2;
                if (i + 3 < value.Length
                    && IsHex(value[i + 3])
                    && IsLegacyCyrillicCode(Convert.ToInt32(value.Substring(i + 1, 3), 16)))
                {
                    hexLength = 3;
                }

                var code = Convert.ToInt32(value.Substring(i + 1, hexLength), 16);
                builder.Append((char)code);
                i += hexLength;
                continue;
            }

            if (value[i] == '=' && i + 1 < value.Length && value[i + 1] == '\r' && i + 2 < value.Length && value[i + 2] == '\n')
            {
                i += 2;
                continue;
            }

            if (value[i] == '=' && i + 1 < value.Length && value[i + 1] == '\n')
            {
                i++;
                continue;
            }

            builder.Append(value[i]);
        }

        return builder.ToString();
    }

    private static bool IsLegacyCyrillicCode(int code) => code is >= 0x400 and <= 0x52F;

    private static bool IsHex(char ch) =>
        char.IsDigit(ch) || (ch >= 'A' && ch <= 'F') || (ch >= 'a' && ch <= 'f');

    private static string CleanVcfText(string value)
    {
        var normalized = value
            .Replace("\\n", " ")
            .Replace("\\N", " ")
            .Replace("\r", " ")
            .Replace("\n", " ")
            .Replace("\\,", ",")
            .Replace("\\;", ";");

        return string.Join(" ",
            normalized.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
                .Where(part => !string.Equals(part, "null", StringComparison.OrdinalIgnoreCase)));
    }

    private void ExportSelectedContacts(string format)
    {
        var selectedContacts = GetCheckedContacts();
        if (selectedContacts.Count == 0)
        {
            MessageBox.Show("Отметьте галочками одну или несколько записей для экспорта.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var saveFileDialog = new SaveFileDialog
        {
            Filter = format == "vcf"
                ? "VCF files (*.vcf)|*.vcf"
                : "CSV files (*.csv)|*.csv|Text files (*.txt)|*.txt",
            DefaultExt = format,
            FileName = format == "vcf" ? "contacts.vcf" : "contacts.csv"
        };

        if (saveFileDialog.ShowDialog(this) != DialogResult.OK)
            return;

        if (format == "vcf")
        {
            ExportContactsAsVcf(selectedContacts, saveFileDialog.FileName);
            MessageBox.Show($"Экспортировано {selectedContacts.Count} контактов в VCF.", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        ExportContactsAsCsv(selectedContacts, saveFileDialog.FileName);
        MessageBox.Show($"Экспортировано {selectedContacts.Count} записей.", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private static void ExportContactsAsCsv(List<Contact> selectedContacts, string filePath)
    {
        using var writer = new StreamWriter(filePath, false, Encoding.UTF8);
        writer.WriteLine("Имя;Телефон;E-mail;Группа;Заметки;Дата рождения");

        foreach (var contact in selectedContacts)
        {
            var birthday = contact.DateOfBirth.HasValue ? contact.DateOfBirth.Value.ToString("dd.MM.yyyy") : string.Empty;
            writer.WriteLine($"{EscapeCsv(contact.Name)};{EscapeCsv(contact.Phone)};{EscapeCsv(contact.Email)};{EscapeCsv(contact.GroupName)};{EscapeCsv(contact.Notes)};{EscapeCsv(birthday)}");
        }
    }

    private static void ExportContactsAsVcf(List<Contact> selectedContacts, string filePath)
    {
        var lines = new List<string>();

        foreach (var contact in selectedContacts)
        {
            var familyName = string.Empty;
            var givenName = string.Empty;
            var additionalName = string.Empty;
            var parts = contact.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length == 1)
            {
                givenName = parts[0];
            }
            else if (parts.Length > 1)
            {
                familyName = parts[^1];
                givenName = parts[0];
                if (parts.Length > 2)
                    additionalName = string.Join(" ", parts[1..^1]);
            }

            var nValue = $"{familyName};{givenName};{additionalName};;";
            var fnValue = contact.Name;

            lines.Add("BEGIN:VCARD");
            lines.Add("VERSION:2.1");
            AddQuotedPrintableLine(lines, $"N;CHARSET=UTF-8;ENCODING=QUOTED-PRINTABLE:{EncodeQuotedPrintable(nValue)}");
            AddQuotedPrintableLine(lines, $"FN;CHARSET=UTF-8;ENCODING=QUOTED-PRINTABLE:{EncodeQuotedPrintable(fnValue)}");

            if (!string.IsNullOrWhiteSpace(contact.Phone))
                lines.Add($"TEL;TYPE=CELL:{EscapeVcf(contact.Phone)}");

            if (!string.IsNullOrWhiteSpace(contact.Email))
                lines.Add($"EMAIL;PREF;WORK:{EscapeVcf(contact.Email)}");

            if (!string.IsNullOrWhiteSpace(contact.GroupName))
                AddQuotedPrintableLine(lines, $"ORG;CHARSET=UTF-8;ENCODING=QUOTED-PRINTABLE:{EncodeQuotedPrintable(contact.GroupName)}");

            if (contact.DateOfBirth.HasValue)
                lines.Add($"BDAY:{contact.DateOfBirth.Value:yyyy-MM-dd}");

            if (!string.IsNullOrWhiteSpace(contact.Notes))
                AddQuotedPrintableLine(lines, $"NOTE;CHARSET=UTF-8;ENCODING=QUOTED-PRINTABLE:{EncodeQuotedPrintable(contact.Notes)}");

            lines.Add("END:VCARD");
        }

        File.WriteAllLines(filePath, lines, Encoding.UTF8);
    }

    private static string EncodeQuotedPrintable(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        var builder = new StringBuilder();
        foreach (var valueByte in Encoding.UTF8.GetBytes(value))
        {
            var code = valueByte;
            if ((code >= 33 && code <= 60) || (code >= 62 && code <= 126))
            {
                builder.Append((char)code);
                continue;
            }

            builder.Append('=').Append(code.ToString("X2"));
        }

        return builder.ToString();
    }

    private static void AddQuotedPrintableLine(List<string> lines, string line)
    {
        while (line.Length > 75)
        {
            var breakAt = 75;
            var escapeStart = line.LastIndexOf('=', breakAt - 1, breakAt);
            if (escapeStart >= 0 && escapeStart + 3 > breakAt)
                breakAt = escapeStart;

            lines.Add(line[..breakAt] + "=");
            line = line[breakAt..];
        }

        lines.Add(line);
    }

    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        value = value.Replace("\r", " ").Replace("\n", " ");
        if (value.Contains(';') || value.Contains('"'))
        {
            value = value.Replace("\"", "\"\"");
            return $"\"{value}\"";
        }

        return value;
    }

    private static string EscapeVcf(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return value
            .Replace("\\", "\\\\")
            .Replace(";", "\\;")
            .Replace(",", "\\,")
            .Replace("\r", string.Empty)
            .Replace("\n", " ");
    }

    private void btnPrint_Click(object sender, EventArgs e)
    {
        var selectedContacts = GetCheckedContacts();
        if (selectedContacts.Count == 0)
        {
            MessageBox.Show("Отметьте галочками одну или несколько записей для печати.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _printContacts = selectedContacts;
        using var printPreviewDialog = new PrintPreviewDialog
        {
            Document = _printDocument,
            WindowState = FormWindowState.Maximized
        };

        printPreviewDialog.ShowDialog(this);
    }

    private void PrintDocument_PrintPage(object sender, PrintPageEventArgs e)
    {
        var g = e.Graphics ?? throw new InvalidOperationException("Графика для печати недоступна.");
        var font = new Font("Segoe UI", 11);
        var headerFont = new Font("Segoe UI", 12, FontStyle.Bold);
        var x = 50f;
        var y = 50f;
        var lineHeight = 24f;

        g.DrawString("Телефонный справочник", headerFont, Brushes.Black, x, y);
        y += 40;

        foreach (var contact in _printContacts)
        {
            var name = contact.Name ?? string.Empty;
            var phone = contact.Phone ?? string.Empty;
            var email = contact.Email ?? string.Empty;
            var groupName = contact.GroupName ?? string.Empty;
            var notes = contact.Notes ?? string.Empty;
            var birthdayText = contact.DateOfBirth is DateTime bd ? bd.ToString("dd.MM.yyyy") : "-";

            g.DrawString($"Имя: {name}", font, Brushes.Black, x, y);
            y += lineHeight;
            g.DrawString($"Телефон: {phone}", font, Brushes.Black, x, y);
            y += lineHeight;
            g.DrawString($"E-mail: {email}", font, Brushes.Black, x, y);
            y += lineHeight;
            g.DrawString($"Группа: {groupName}", font, Brushes.Black, x, y);
            y += lineHeight;
            g.DrawString($"Дата рождения: {birthdayText}", font, Brushes.Black, x, y);
            y += lineHeight;
            g.DrawString($"Заметки: {notes}", font, Brushes.Black, x, y);
            y += lineHeight * 2;

            if (y > e.PageBounds.Height - 100)
            {
                e.HasMorePages = true;
                return;
            }
        }

        e.HasMorePages = false;
    }

    private void btnSearch_Click(object sender, EventArgs e)
    {
        LoadContacts(txtSearch.Text.Trim());
    }

    private void btnRefresh_Click(object sender, EventArgs e)
    {
        LoadContacts(txtSearch.Text.Trim());
    }

    private void btnClear_Click(object sender, EventArgs e)
    {
        txtSearch.Clear();
        LoadContacts();
    }

    private void txtSearch_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            LoadContacts(txtSearch.Text.Trim());
            e.Handled = true;
        }
    }

    private void dgvContacts_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
    {
        if (e.ColumnIndex < 0)
            return;

        var dataSource = dgvContacts.DataSource as List<Contact>;
        if (dataSource is null)
            return;

        var columnName = dgvContacts.Columns[e.ColumnIndex].DataPropertyName;
        if (string.IsNullOrEmpty(columnName) || columnName == "Id")
            return;

        var ascending = !_sortDirections.GetValueOrDefault(columnName);
        _sortDirections[columnName] = ascending;

        IOrderedEnumerable<Contact> sorted = columnName switch
        {
            "Name" => Sort(dataSource, c => c.Name, ascending),
            "Phone" => Sort(dataSource, c => c.Phone, ascending),
            "Email" => Sort(dataSource, c => c.Email, ascending),
            "GroupName" => Sort(dataSource, c => c.GroupName, ascending),
            "Notes" => Sort(dataSource, c => c.Notes, ascending),
            "DateOfBirth" => ascending
                ? dataSource.OrderBy(c => c.DateOfBirth ?? DateTime.MaxValue)
                : dataSource.OrderByDescending(c => c.DateOfBirth ?? DateTime.MinValue),
            _ => dataSource.OrderBy(c => c.Name)
        };

        dgvContacts.DataSource = sorted.ToList();
    }

    private static IOrderedEnumerable<Contact> Sort(
        IEnumerable<Contact> contacts,
        Func<Contact, string> selector,
        bool ascending) =>
        ascending
            ? contacts.OrderBy(selector, StringComparer.CurrentCultureIgnoreCase)
            : contacts.OrderByDescending(selector, StringComparer.CurrentCultureIgnoreCase);

    private void btnSelectAll_Click(object sender, EventArgs e)
    {
        dgvContacts.EndEdit();
        if (dgvContacts.Rows.Count == 0)
        {
            btnSelectAll.Text = "Выделить всё";
            return;
        }

        var selectAll = btnSelectAll.Text != "Отменить выбор";

        _updatingCheckboxes = true;
        try
        {
            foreach (DataGridViewRow row in dgvContacts.Rows)
            {
                var id = Convert.ToInt32(row.Cells["Id"].Value);
                row.Cells["SelectForOutput"].Value = selectAll;
                if (selectAll)
                    _checkedContactIds.Add(id);
                else
                    _checkedContactIds.Remove(id);
            }
        }
        finally
        {
            _updatingCheckboxes = false;
        }

        var checkboxColumn = dgvContacts.Columns["SelectForOutput"];
        if (checkboxColumn is not null)
            dgvContacts.InvalidateColumn(checkboxColumn.Index);
        btnSelectAll.Text = selectAll ? "Отменить выбор" : "Выделить всё";
        btnSelectAll.Refresh();
    }

    private void dgvContacts_CurrentCellDirtyStateChanged(object? sender, EventArgs e)
    {
        if (dgvContacts.IsCurrentCellDirty &&
            dgvContacts.CurrentCell is DataGridViewCheckBoxCell)
        {
            dgvContacts.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }
    }

    private void dgvContacts_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
    {
        if (_updatingCheckboxes ||
            e.RowIndex < 0 ||
            e.ColumnIndex < 0 ||
            dgvContacts.Columns[e.ColumnIndex].Name != "SelectForOutput")
        {
            return;
        }

        var row = dgvContacts.Rows[e.RowIndex];
        var id = Convert.ToInt32(row.Cells["Id"].Value);
        if (Convert.ToBoolean(row.Cells["SelectForOutput"].Value ?? false))
            _checkedContactIds.Add(id);
        else
            _checkedContactIds.Remove(id);

        UpdateSelectAllButtonText();
    }

    private static bool IsRowChecked(DataGridViewRow row) =>
        Convert.ToBoolean(row.Cells["SelectForOutput"].Value ?? false);

    private void UpdateSelectAllButtonText()
    {
        var allRowsChecked = dgvContacts.Rows.Count > 0 &&
            dgvContacts.Rows
                .Cast<DataGridViewRow>()
                .All(IsRowChecked);

        btnSelectAll.Text = allRowsChecked ? "Отменить выбор" : "Выделить всё";
    }
}
