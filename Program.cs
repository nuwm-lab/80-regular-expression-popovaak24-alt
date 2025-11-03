using System;
using System.Text.RegularExpressions;


// Перевірка: чи заданий текст містить коректні ORCID-ідентифікатори
// Підтримуються формати:
//  - 0000-0000-0000-0000 (останній символ може бути цифрою або 'X')
//  - https://orcid.org/0000-0000-0000-0000 (підтримка http/https, опційного www)
// Додатково виконується перевірка контрольної цифри за ISO 7064 (Mod 11-2)
class Program
{
    private const int MaxChars = 1_000_000; // Максимальна кількість символів для обробки
    // Попередньо скомпільовані регулярні вирази для ефективності
    private static readonly Regex OrcidRegex = new(
        @"\b(?:https?://(?:www\.)?orcid\.org/)?(\d{4}-\d{4}-\d{4}-\d{3}[\dx])\b",
        RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);

    private static readonly Regex OrcidFormatRegex = new(
        @"^[0-9]{4}-[0-9]{4}-[0-9]{4}-[0-9]{3}[0-9X]$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        // Зчитуємо ввід з обробкою виключень і обмеженням розміру
        string input = string.Empty;
        bool wasTruncated = false;
        try
        {
            if (Console.IsInputRedirected)
            {
                // Безпечне читання великих потоків з обмеженням MaxChars
                var sb = new System.Text.StringBuilder(Math.Min(65536, MaxChars));
                var reader = Console.In;
                var buffer = new char[8192];
                int n;
                while ((n = reader.Read(buffer, 0, buffer.Length)) > 0)
                {
                    int toAppend = Math.Min(n, MaxChars - sb.Length);
                    if (toAppend > 0)
                        sb.Append(buffer, 0, toAppend);
                    if (sb.Length >= MaxChars)
                    {
                        wasTruncated = true;
                        break; // припиняємо читання, решту ігноруємо
                    }
                }
                input = sb.ToString();
            }
            else
            {
                // Читаємо один рядок, щоб не блокуватися очікуванням EOF
                input = Console.ReadLine() ?? string.Empty;
                if (input.Length > MaxChars)
                {
                    input = input.Substring(0, MaxChars);
                    wasTruncated = true;
                }
            }
        }
        catch (System.IO.IOException ex)
        {
            Console.WriteLine("Помилка читання вводу: " + ex.Message);
            return;
        }
        catch (OutOfMemoryException)
        {
            Console.WriteLine($"Помилка: вхідний текст занадто великий (> {MaxChars:N0} символів). Спробуйте подати менший фрагмент або файл частинами.");
            return;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Непередбачена помилка при читанні вводу: " + ex.Message);
            return;
        }
        // Якщо ввід пустий — підставимо приклад (щоб було з чим тестувати)
        if (string.IsNullOrWhiteSpace(input))
        {
            input = "Стаття автора має ORCID: https://orcid.org/0000-0002-1825-0097. Інший приклад: 0000-0003-1415-926X.";
            Console.WriteLine("(Використовується прикладовий текст, оскільки ввід пустий)");
            Console.WriteLine(input);
        }
        else
        {
            if (wasTruncated)
                Console.WriteLine($"Увага: текст занадто довгий. Оброблено лише перші {MaxChars:N0} символів.");

            // Виявлення проблем з кодуванням: символ U+FFFD (�) з'являється при некоректному декодуванні
            if (input.IndexOf('\uFFFD') >= 0)
                Console.WriteLine("Увага: виявлено символи заміни (�). Можливі проблеми з кодуванням вводу. Рекомендовано UTF-8 або PowerShell 7+.");
        }

        // Нормалізуємо ввід (уніфікуємо дефіси), а потім виконуємо пошук ORCID (простий та у складі URL)
        // Група 1 завжди міститиме сам ідентифікатор у вигляді 0000-0000-0000-0000
        var normalizedInput = NormalizeHyphens(input);
        MatchCollection matches = OrcidRegex.Matches(normalizedInput);

        var foundAll = new System.Collections.Generic.List<string>();
        var valid = new System.Collections.Generic.List<string>();
        var invalid = new System.Collections.Generic.List<string>();

        foreach (Match m in matches)
        {
            var id = m.Groups[1].Value.ToUpperInvariant();
            if (!string.IsNullOrWhiteSpace(id))
            {
                foundAll.Add(id);
                if (IsValidOrcid(id)) valid.Add(id);
                else invalid.Add(id);
            }
        }

        if (valid.Count > 0)
        {
            Console.WriteLine("Результат: Знайдено коректні ORCID-ідентифікатори (перевірено за контрольним розрядом):");
            foreach (var id in valid)
                Console.WriteLine(" - " + id);
        }
        else
        {
            if (foundAll.Count > 0)
            {
                Console.WriteLine("Результат: Знайдено схожі на ORCID рядки, але з некоректною контрольною цифрою:");
                foreach (var id in invalid)
                    Console.WriteLine(" - " + id);
            }
            else
            {
                Console.WriteLine("Результат: У тексті не виявлено ORCID-ідентифікаторів.");
            }
        }
    }

    // Перевірка контрольної цифри ORCID за ISO 7064 (Mod 11-2)
    static bool IsValidOrcid(string orcid)
    {
        if (string.IsNullOrWhiteSpace(orcid)) return false;

        // Нормалізуємо формат: XXXX-XXXX-XXXX-XXXX
        orcid = NormalizeHyphens(orcid.ToUpperInvariant());
        if (!OrcidFormatRegex.IsMatch(orcid))
            return false;

        var digits = orcid.Replace("-", "");
        if (digits.Length != 16) return false;

        int total = 0;
        for (int i = 0; i < 15; i++)
        {
            if (!char.IsDigit(digits[i])) return false;
            int d = digits[i] - '0';
            total = (total + d) * 2;
        }

        int remainder = total % 11;
        int result = (12 - remainder) % 11;
        char expected = result == 10 ? 'X' : (char)('0' + result);

        char last = char.ToUpperInvariant(digits[15]);
        return last == expected;
    }

    // Замінює різні види дефісів/тире на звичайний ASCII '-'
    private static string NormalizeHyphens(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        // U+2010 Hyphen, U+2011 NB Hyphen, U+2012 Figure Dash, U+2013 En Dash,
        // U+2014 Em Dash, U+2212 Minus Sign
        return s
            .Replace('\u2010', '-')
            .Replace('\u2011', '-')
            .Replace('\u2012', '-')
            .Replace('\u2013', '-')
            .Replace('\u2014', '-')
            .Replace('\u2212', '-');
    }
}
