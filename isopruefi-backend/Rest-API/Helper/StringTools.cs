namespace Rest_API.Helper;

/// <summary>
///     This class provides utility methods for string manipulation.
/// </summary>
public static class StringTools


{
    /// <summary>
    ///     Takes a string input and sanitizes it by removing any characters that are not alphanumeric, spaces, or underscores.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public static string SanitizeString(this string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;

        // Remove any characters that are not alphanumeric, spaces, or underscores
        var sanitized = new string(input.Where(c => char.IsLetterOrDigit(c) || c == ' ' || c == '_').ToArray());

        // Trim leading and trailing whitespace
        return sanitized.Trim();
    }
}