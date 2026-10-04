using Microsoft.Data.Sqlite;

namespace Phonebook;

public class Contact
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
}

public class PhonebookDatabase
{
    private readonly string _connectionString;

    public PhonebookDatabase()
    {
        var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "phonebook.db");
        _connectionString = $"Data Source={dbPath}";
    }

    public void Initialize()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Contacts (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Phone TEXT NOT NULL,
                Email TEXT,
                Notes TEXT,
                Birthday TEXT,
                GroupName TEXT
            );";

        command.ExecuteNonQuery();

        command.CommandText = "PRAGMA table_info(Contacts);";
        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using (var reader = command.ExecuteReader())
        {
            while (reader.Read())
                columns.Add(reader.GetString(1));
        }

        if (!columns.Contains("Birthday"))
        {
            command.CommandText = "ALTER TABLE Contacts ADD COLUMN Birthday TEXT;";
            command.ExecuteNonQuery();
        }

        if (!columns.Contains("GroupName"))
        {
            command.CommandText = "ALTER TABLE Contacts ADD COLUMN GroupName TEXT;";
            command.ExecuteNonQuery();
        }
    }

    public List<Contact> GetAll(string searchText = "")
    {
        var results = new List<Contact>();
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        if (string.IsNullOrWhiteSpace(searchText))
        {
            command.CommandText = @"
                SELECT Id, Name, Phone, Email, Notes, Birthday, GroupName
                FROM Contacts
                ORDER BY Name;";
        }
        else
        {
            command.CommandText = @"
                SELECT Id, Name, Phone, Email, Notes, Birthday, GroupName
                FROM Contacts
                WHERE Name LIKE @search
                   OR Phone LIKE @search
                   OR Email LIKE @search
                   OR Notes LIKE @search
                   OR Birthday LIKE @search
                   OR GroupName LIKE @search
                ORDER BY Name;";
            command.Parameters.AddWithValue("@search", $"%{searchText}%");
        }

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            DateTime? birthday = null;
            if (!reader.IsDBNull(5) && DateTime.TryParse(reader.GetString(5), out var parsedBirthday))
            {
                birthday = parsedBirthday;
            }

            results.Add(new Contact
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Phone = reader.GetString(2),
                Email = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                Notes = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                DateOfBirth = birthday,
                GroupName = reader.IsDBNull(6) ? string.Empty : reader.GetString(6)
            });
        }

        return results;
    }

    public List<string> GetGroups()
    {
        var groups = new List<string>();
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT DISTINCT TRIM(GroupName) COLLATE NOCASE
            FROM Contacts
            WHERE GroupName IS NOT NULL AND TRIM(GroupName) <> ''
            ORDER BY TRIM(GroupName) COLLATE NOCASE;";

        using var reader = command.ExecuteReader();
        while (reader.Read())
            groups.Add(reader.GetString(0));

        return groups;
    }

    public void AddContact(Contact contact)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO Contacts (Name, Phone, Email, Notes, Birthday, GroupName)
            VALUES (@name, @phone, @email, @notes, @birthday, @groupName);";

        command.Parameters.AddWithValue("@name", contact.Name);
        command.Parameters.AddWithValue("@phone", contact.Phone);
        command.Parameters.AddWithValue("@email", string.IsNullOrWhiteSpace(contact.Email) ? DBNull.Value : contact.Email);
        command.Parameters.AddWithValue("@notes", string.IsNullOrWhiteSpace(contact.Notes) ? DBNull.Value : contact.Notes);
        command.Parameters.AddWithValue("@birthday", contact.DateOfBirth.HasValue ? contact.DateOfBirth.Value.ToString("yyyy-MM-dd") : DBNull.Value);
        command.Parameters.AddWithValue("@groupName", string.IsNullOrWhiteSpace(contact.GroupName) ? DBNull.Value : contact.GroupName.Trim());

        command.ExecuteNonQuery();
    }

    public Contact? FindByNameAndPhone(string name, string phone)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT Id, Name, Phone, Email, Notes, Birthday, GroupName
            FROM Contacts;";

        var normalizedName = NormalizeContactName(name);
        var normalizedPhone = PhoneNumberFormatter.Normalize(phone);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var existingName = reader.GetString(1);
            var existingPhone = reader.GetString(2);
            if (!string.Equals(NormalizeContactName(existingName), normalizedName, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(PhoneNumberFormatter.Normalize(existingPhone), normalizedPhone, StringComparison.Ordinal))
                continue;

            return new Contact
            {
                Id = reader.GetInt32(0),
                Name = existingName,
                Phone = existingPhone,
                Email = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                Notes = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                DateOfBirth = !reader.IsDBNull(5) && DateTime.TryParse(reader.GetString(5), out var parsedBirthday) ? parsedBirthday : null,
                GroupName = reader.IsDBNull(6) ? string.Empty : reader.GetString(6)
            };
        }

        return null;
    }

    public bool MergeDuplicateContact(Contact importedContact)
    {
        var existing = FindByNameAndPhone(importedContact.Name, importedContact.Phone);
        if (existing is null)
        {
            AddContact(importedContact);
            return true;
        }

        var update = false;
        var email = existing.Email;
        var notes = existing.Notes;
        var group = existing.GroupName;
        var birthday = existing.DateOfBirth;

        if (string.IsNullOrWhiteSpace(existing.Email) && !string.IsNullOrWhiteSpace(importedContact.Email))
        {
            email = importedContact.Email;
            update = true;
        }

        var mergedNotes = MergeDistinctText(existing.Notes, importedContact.Notes);
        if (!string.Equals(mergedNotes, existing.Notes, StringComparison.Ordinal))
        {
            notes = mergedNotes;
            update = true;
        }

        if (string.IsNullOrWhiteSpace(existing.GroupName) && !string.IsNullOrWhiteSpace(importedContact.GroupName))
        {
            group = importedContact.GroupName;
            update = true;
        }

        if (!existing.DateOfBirth.HasValue && importedContact.DateOfBirth.HasValue)
        {
            birthday = importedContact.DateOfBirth;
            update = true;
        }

        if (!update)
            return false;

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            UPDATE Contacts
            SET Email = @email,
                Notes = @notes,
                Birthday = @birthday,
                GroupName = @groupName
            WHERE Id = @id;";

        command.Parameters.AddWithValue("@id", existing.Id);
        command.Parameters.AddWithValue("@email", string.IsNullOrWhiteSpace(email) ? DBNull.Value : email);
        command.Parameters.AddWithValue("@notes", string.IsNullOrWhiteSpace(notes) ? DBNull.Value : notes);
        command.Parameters.AddWithValue("@birthday", birthday.HasValue ? birthday.Value.ToString("yyyy-MM-dd") : DBNull.Value);
        command.Parameters.AddWithValue("@groupName", string.IsNullOrWhiteSpace(group) ? DBNull.Value : group.Trim());
        command.ExecuteNonQuery();
        return false;
    }

    private static string NormalizeContactName(string name) =>
        string.Join(" ", name.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private static string MergeDistinctText(string existing, string imported)
    {
        var existingValue = CleanImportedText(existing);
        var importedValue = CleanImportedText(imported);
        if (string.IsNullOrEmpty(existingValue))
            return importedValue;
        if (string.IsNullOrEmpty(importedValue)
            || existingValue.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
                .Any(value => string.Equals(value.Trim(), importedValue, StringComparison.OrdinalIgnoreCase)))
            return existingValue;

        return $"{existingValue}{Environment.NewLine}{importedValue}";
    }

    private static string CleanImportedText(string value) =>
        string.Join(" ", value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
            .Where(part => !string.Equals(part, "null", StringComparison.OrdinalIgnoreCase)));

    public void DeleteContact(int id)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Contacts WHERE Id = @id;";
        command.Parameters.AddWithValue("@id", id);
        command.ExecuteNonQuery();
    }

    public void UpdateContact(Contact contact)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            UPDATE Contacts
            SET Name = @name,
                Phone = @phone,
                Email = @email,
                Notes = @notes,
                Birthday = @birthday,
                GroupName = @groupName
            WHERE Id = @id;";

        command.Parameters.AddWithValue("@id", contact.Id);
        command.Parameters.AddWithValue("@name", contact.Name);
        command.Parameters.AddWithValue("@phone", contact.Phone);
        command.Parameters.AddWithValue("@email", string.IsNullOrWhiteSpace(contact.Email) ? DBNull.Value : contact.Email);
        command.Parameters.AddWithValue("@notes", string.IsNullOrWhiteSpace(contact.Notes) ? DBNull.Value : contact.Notes);
        command.Parameters.AddWithValue("@birthday", contact.DateOfBirth.HasValue ? contact.DateOfBirth.Value.ToString("yyyy-MM-dd") : DBNull.Value);
        command.Parameters.AddWithValue("@groupName", string.IsNullOrWhiteSpace(contact.GroupName) ? DBNull.Value : contact.GroupName.Trim());

        command.ExecuteNonQuery();
    }
}
