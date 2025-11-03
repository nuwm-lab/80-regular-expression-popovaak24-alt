using System;
using System.Text.RegularExpressions;


// Перевірка: чи заданий текст є HTML-кодом і містить теги <html>, <form>, <h1>
class Program
{
    private const int MaxChars = 1_000_000; // Максимальна кількість символів для обробки
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
            input = "<html><head><title>Приклад</title></head><body><h1>Заголовок</h1><form action=\"\">...</form></body></html>";
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

        // Перевірка наявності HTML-тегів <html>, <form>, <h1>
        string[] tags = { "html", "form", "h1" };
        var missing = new System.Collections.Generic.List<string>();
        foreach (var tag in tags)
        {
            // Шукаємо відкриваючий тег з можливими пробілами, атрибутами і незалежно від регістру
            string pattern = $"<\\s*{tag}\\b[^>]*>";
            if (!Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline))
            {
                missing.Add(tag);
            }
        }
        if (missing.Count == 0)
        {
            Console.WriteLine("Результат: Текст містить HTML-код — знайдено теги <html>, <form>, <h1>.");
        }
        else
        {
            Console.WriteLine("Результат: Текст НЕ містить необхідних HTML-тегів. Відсутні: " + string.Join(", ", missing.ConvertAll(s => $"<{s}>") ) );
        }
    }
}
