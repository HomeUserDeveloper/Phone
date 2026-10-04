namespace Phonebook;

internal static class PhoneNumberFormatter
{
    public static string Normalize(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return string.Empty;

        return new string(phone.Where(char.IsDigit).ToArray());
    }

    public static string Format(string phone)
    {
        var digits = Normalize(phone);
        if (digits.Length == 0)
            return phone.TrimStart().StartsWith('+') ? "+" : string.Empty;

        var hasCountryCode = phone.TrimStart().StartsWith('+');
        var countryCodeLength = digits.Length > 10
            ? Math.Min(digits.Length - 10, 3)
            : hasCountryCode ? Math.Min(digits.Length, 3) : 0;
        var countryCode = digits[..countryCodeLength];
        var nationalNumber = digits[countryCodeLength..];
        var nationalGroups = FormatNationalNumber(nationalNumber);

        if (countryCodeLength == 0)
            return nationalGroups;

        return string.IsNullOrEmpty(nationalGroups)
            ? $"+({countryCode})"
            : $"+({countryCode}) {nationalGroups}";
    }

    public static int GetCaretPosition(string formattedPhone, int digitCount)
    {
        if (digitCount <= 0)
            return 0;

        var digitsSeen = 0;
        for (var i = 0; i < formattedPhone.Length; i++)
        {
            if (char.IsDigit(formattedPhone[i]) && ++digitsSeen == digitCount)
                return i + 1;
        }

        return formattedPhone.Length;
    }

    private static string FormatNationalNumber(string digits)
    {
        int[] groupSizes = [3, 3, 2, 2];
        var groups = new List<string>();
        var offset = 0;

        foreach (var groupSize in groupSizes)
        {
            if (offset >= digits.Length)
                break;

            var length = Math.Min(groupSize, digits.Length - offset);
            groups.Add(digits.Substring(offset, length));
            offset += length;
        }

        if (offset < digits.Length)
            groups.Add(digits[offset..]);

        return string.Join("-", groups);
    }
}
