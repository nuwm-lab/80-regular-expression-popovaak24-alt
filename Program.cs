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
