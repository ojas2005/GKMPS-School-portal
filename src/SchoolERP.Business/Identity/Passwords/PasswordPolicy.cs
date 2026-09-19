namespace SchoolERP.Business.Identity.Passwords;

/// <summary>
/// What makes a password acceptable. Length matters most, so the rule is at least 10
/// characters -- plus refusing the passwords attackers try first: common ones (even dressed
/// up as "P@ssw0rd123"), keyboard runs and repeats, and anything built from the person's
/// own name or login ID or the school's name. No "must contain a symbol" rules: they make
/// passwords harder to remember without making them much harder to guess.
/// </summary>
public static class PasswordPolicy
{
    public const int MinLength = 10;
    public const int MaxLength = 128;

    // The words behind most guessed passwords. Matched after undoing common disguises
    // (P@ssw0rd -> password) and dropping digits and symbols (Welcome@2024 -> welcome).
    private static readonly HashSet<string> WeakWords = new(StringComparer.Ordinal)
    {
        "password", "passwd", "pass", "welcome", "admin", "administrator", "root", "user", "login", "letmein",
        "qwerty", "qwertyuiop", "asdfgh", "asdfghjkl", "zxcvbnm", "abc", "abcd", "abcdef", "abcdefgh", "iloveyou",
        "sunshine", "princess", "dragon", "monkey", "football", "cricket", "baseball", "soccer", "master", "shadow",
        "superman", "batman", "starwars", "freedom", "trustno", "whatever", "secret", "hello", "test", "testing",
        "default", "changeme", "guest", "school", "student", "teacher", "principal", "parent", "library", "india",
        "bharat", "hindustan", "ganesh", "krishna", "shiva", "jaishreeram", "om", "sairam", "love", "baby", "god",
    };

    private static readonly string[] KeyboardRuns = ["qwertyuiop", "asdfghjkl", "zxcvbnm", "1234567890", "0987654321", "abcdefghijklmnopqrstuvwxyz", "1qaz2wsx3edc", "1q2w3e4r5t6y"];

    /// <summary>Problems with the password, in plain words -- empty when it's acceptable.</summary>
    public static IReadOnlyList<string> Check(string password, params string?[] personalWords)
    {
        var problems = new List<string>();
        if (string.IsNullOrEmpty(password) || password.Length < MinLength)
        {
            problems.Add($"Use at least {MinLength} characters. A few unrelated words make a strong password that's easy to remember.");
            return problems;
        }
        if (password.Length > MaxLength)
        {
            problems.Add($"Use at most {MaxLength} characters.");
            return problems;
        }

        var lower = password.ToLowerInvariant();
        var undisguised = Undisguise(lower);
        // The word at the heart of it: drop the digits and symbols tacked on either end
        // ("Welcome@2024" -> "welcome"), then undo disguises inside it ("p@ssw0rd" -> "password").
        var letters = new string(Undisguise(TrimNonLetters(lower)).Where(char.IsLetter).ToArray());

        if (password.Distinct().Count() <= 3)
            problems.Add("Avoid repeating the same few characters.");
        else if (KeyboardRuns.Any(run => IsRunOf(lower, run) || IsRunOf(Reverse(lower), run)))
            problems.Add("Avoid keyboard patterns and number sequences like 1234567890 or qwertyuiop.");
        else if (letters.Length <= 3 || WeakWords.Contains(letters) || WeakWords.Contains(Reverse(letters)))
            problems.Add("That's one of the passwords people guess first, even with numbers or symbols added. Try a few unrelated words instead.");

        foreach (var word in personalWords.SelectMany(Tokens).Distinct())
        {
            if (word.Length >= 4 && (lower.Contains(word) || undisguised.Contains(word)))
            {
                problems.Add("Don't use your name, your login ID or the school's name in your password.");
                break;
            }
        }
        return problems;
    }

    private static IEnumerable<string> Tokens(string? text) =>
        string.IsNullOrWhiteSpace(text)
            ? []
            : text.ToLowerInvariant().Split([' ', '.', '_', '-', '@', '+'], StringSplitOptions.RemoveEmptyEntries);

    // Undoes the substitutions people use to "strengthen" a word: @->a, 0->o, 1->i, 3->e, $->s, ...
    private static string Undisguise(string s) => new(s.Select(c => c switch
    {
        '@' or '4' => 'a', '0' => 'o', '1' or '!' or '|' => 'i', '3' => 'e', '$' or '5' => 's', '7' or '+' => 't', '8' => 'b', '9' => 'g',
        _ => c
    }).ToArray());

    // The whole password is (part of) the run, e.g. "1234567890", "qwertyuiopa" -> no, "34567890123".
    private static bool IsRunOf(string password, string run)
    {
        var chars = new string(password.Where(char.IsLetterOrDigit).ToArray());
        if (chars.Length < 6) return false;
        var doubled = run + run;
        return doubled.Contains(chars) || chars.Contains(run[..Math.Min(run.Length, 8)]) && chars.Length - run.Length < 3;
    }

    private static string Reverse(string s) => new(s.Reverse().ToArray());

    private static string TrimNonLetters(string s)
    {
        int start = 0, end = s.Length;
        while (start < end && !char.IsLetter(s[start])) start++;
        while (end > start && !char.IsLetter(s[end - 1])) end--;
        return s[start..end];
    }
}
