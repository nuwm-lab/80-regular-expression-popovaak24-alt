using System;
using System.Text.RegularExpressions;

// Перевірка чи заданий текст містить HTML-теги <html>, <form>, <h1>
// Використовуємо класи з простору імен System.Text.RegularExpressions
class Program
{
	static void Main()
	{
		Console.OutputEncoding = System.Text.Encoding.UTF8;

		// Зчитуємо весь ввід — дозволяє вставляти багаторядковий текст у консоль
		string input = Console.In.ReadToEnd();

		// Якщо ввід пустий — підставимо приклад (щоб було з чим тестувати)
		if (string.IsNullOrWhiteSpace(input))
		{
			input = "<html><head><title>Приклад</title></head><body><h1>Заголовок</h1><form action=\"\">...</form></body></html>";
			Console.WriteLine("(Використовується прикладовий текст, оскільки ввід пустий)");
			Console.WriteLine(input);
		}

	// Теги, які потрібно знайти
		string[] tags = { "html", "form", "h1" };
		var missing = new System.Collections.Generic.List<string>();

	// (Увімкніть додатковий вивід для налагодження, якщо потрібно)

		foreach (var tag in tags)
		{
			// Строгіший шаблон: перевіряємо відкриваючий або закриваючий тег
			// - /? допускає як відкриваючі, так і закриваючі теги
			// - \b гарантує межу після імені тега (не знайде частини слів)
			// - Заміна жадібного [^>]* на нежадібний [^>]*? щоб уникнути переузгодження між тегами
			string pattern = $"<\\s*/?\\s*{tag}\\b[^>]*?>";
			var options = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;
			if (!Regex.IsMatch(input, pattern, options))
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

		// Додаткові детектори: одночасне знаходження різних шаблонів (приклад: ORCID та IPv4)
		var detectors = new System.Collections.Generic.Dictionary<string, string>()
		{
			// ORCID: 4 групи по 4 символи, останній символ може бути цифрою або 'X' (checksum)
			{ "ORCID", @"\b\d{4}-\d{4}-\d{4}-\d{3}[\dX]\b" },
			// IPv4: строгий шаблон для чисел 0-255
			{ "IPv4", @"\b(?:(?:25[0-5]|2[0-4]\d|1?\d{1,2})\.){3}(?:25[0-5]|2[0-4]\d|1?\d{1,2})\b" }
		};

		var regexOptions = RegexOptions.CultureInvariant;

		Console.WriteLine();
		Console.WriteLine("Детектори шаблонів:");
		foreach (var kv in detectors)
		{
			string name = kv.Key;
			string pattern = kv.Value;
			var matches = Regex.Matches(input, pattern, regexOptions);
			int total = matches.Count;
			var unique = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
			foreach (System.Text.RegularExpressions.Match m in matches)
			{
				if (!string.IsNullOrEmpty(m.Value)) unique.Add(m.Value);
			}

			Console.WriteLine($"- {name}: знайдено {total}, унікальних значень: {unique.Count}");
			if (unique.Count > 0)
			{
				Console.WriteLine("  Список унікальних:");
				foreach (var v in unique)
				{
					Console.WriteLine($"    {v}");
				}
			}
		}
	}
}

