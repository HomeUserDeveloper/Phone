using System.Reflection;
using Microsoft.Data.Sqlite;
using Phonebook;

var tests = new (string Name, Action Run)[]
{
    ("Detect semicolon CSV delimiter", DetectSemicolonDelimiter),
    ("Detect comma CSV delimiter", DetectCommaDelimiter),
    ("Parse quoted CSV fields and multiline values", ParseQuotedCsv),
    ("Reject unclosed CSV quote", RejectUnclosedQuote),
    ("Round-trip UTF-8 quoted-printable", RoundTripQuotedPrintable),
    ("Decode legacy Cyrillic quoted-printable", DecodeLegacyQuotedPrintable),
    ("Normalize phone number", NormalizePhone),
    ("Add and merge contacts in SQLite", AddAndMergeContact)
};

var failed = 0;
foreach (var test in tests)
{
    try
    {
        test.Run();
        Console.WriteLine($"PASS {test.Name}");
    }
    catch (Exception ex)
    {
        failed++;
        Console.Error.WriteLine($"FAIL {test.Name}: {ex.GetBaseException().Message}");
    }
}

Console.WriteLine($"{tests.Length - failed}/{tests.Length} tests passed.");
return failed == 0 ? 0 : 1;

static void DetectSemicolonDelimiter() =>
    AssertEqual(';', InvokeStatic<char>("DetectCsvDelimiter", "Name;Phone\nAlex;123"));

static void DetectCommaDelimiter() =>
    AssertEqual(',', InvokeStatic<char>("DetectCsvDelimiter", "Name,Phone\nAlex,123"));

static void ParseQuotedCsv()
{
    const string csv = "Имя;Телефон;Заметки\r\n\"Иван; Иванов\";+7 900;\"Первая строка\nВторая, \"\"важная\"\"\"";
    var rows = InvokeStatic<List<List<string>>>("ParseCsv", csv, ';');

    AssertEqual(2, rows.Count);
    AssertEqual("Иван; Иванов", rows[1][0]);
    AssertEqual("+7 900", rows[1][1]);
    AssertEqual("Первая строка\nВторая, \"важная\"", rows[1][2]);
}

static void RejectUnclosedQuote()
{
    var exception = AssertThrows<TargetInvocationException>(() => InvokeStatic<List<List<string>>>("ParseCsv", "Name;Phone\n\"Alex;123", ';'));
    Assert(exception.InnerException is FormatException, "Expected a FormatException for an unclosed CSV quote.");
}

static void RoundTripQuotedPrintable()
{
    const string source = "Алексей, ёж";
    var encoded = InvokeStatic<string>("EncodeQuotedPrintable", source);
    Assert(encoded.StartsWith("=D0=90", StringComparison.Ordinal), "Expected UTF-8 bytes in quoted-printable output.");
    AssertEqual(source, InvokeStatic<string>("DecodeQuotedPrintable", encoded));
}

static void DecodeLegacyQuotedPrintable() =>
    AssertEqual("Алексей", InvokeStatic<string>("DecodeQuotedPrintable", "=410=43B=435=43A=441=435=439"));

static void NormalizePhone() =>
    AssertEqual("79001234567", InvokeStaticOn<string>(
        typeof(Form1).Assembly.GetType("Phonebook.PhoneNumberFormatter")!,
        "Normalize",
        "+7 (900) 123-45-67"));

static void AddAndMergeContact()
{
    var databasePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "phonebook.db");
    SqliteConnection.ClearAllPools();
    DeleteTestDatabaseFiles(databasePath);

    try
    {
        var database = new PhonebookDatabase();
        database.Initialize();
        database.AddContact(new Contact { Name = "Test User", Phone = "79001234567" });

        var added = database.MergeDuplicateContact(new Contact
        {
            Name = " Test   User ",
            Phone = "+7 (900) 123-45-67",
            Email = "test@example.invalid",
            GroupName = "Tests"
        });

        Assert(!added, "A matching contact must be merged, not inserted.");
        var contact = database.FindByNameAndPhone("Test User", "79001234567");
        Assert(contact is not null, "Expected the imported contact to remain in the database.");
        AssertEqual("test@example.invalid", contact!.Email);
        AssertEqual("Tests", contact.GroupName);
        AssertEqual(1, database.GetAll().Count);
    }
    finally
    {
        SqliteConnection.ClearAllPools();
        DeleteTestDatabaseFiles(databasePath);
    }
}

static void DeleteTestDatabaseFiles(string databasePath)
{
    foreach (var suffix in new[] { string.Empty, "-shm", "-wal" })
    {
        var path = databasePath + suffix;
        if (File.Exists(path))
            File.Delete(path);
    }
}

static T InvokeStatic<T>(string methodName, params object?[] arguments) =>
    InvokeStaticOn<T>(typeof(Form1), methodName, arguments);

static T InvokeStaticOn<T>(Type type, string methodName, params object?[] arguments)
{
    var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
        ?? throw new InvalidOperationException($"Method {methodName} was not found.");
    return (T)(method.Invoke(null, arguments) ?? throw new InvalidOperationException($"Method {methodName} returned null."));
}

static void Assert(bool condition, string message)
{
    if (!condition)
        throw new InvalidOperationException(message);
}

static void AssertEqual<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
        throw new InvalidOperationException($"Expected '{expected}', got '{actual}'.");
}

static TException AssertThrows<TException>(Action action) where TException : Exception
{
    try
    {
        action();
    }
    catch (TException exception)
    {
        return exception;
    }

    throw new InvalidOperationException($"Expected {typeof(TException).Name}.");
}
