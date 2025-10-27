using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace GadgetApp.Models
{
    // Представляє модель гаджета з його основними характеристиками.
    public class Gadget
    {
        // --- ВЛАСТИВОСТІ КЛАСУ ---
        public string Name { get; set; }
        public string Manufacturer { get; set; }
        public decimal Price { get; set; }
        public GadgetType Type { get; set; }
        public string ScreenSize { get; set; }
        public List<GadgetFeature> Features { get; set; }
        // Допоміжна властивість, що вказує, чи є цей об'єкт новоствореним.
        public bool IsNew { get; set; }
        
        // Обчислювана властивість для відображення функцій у таблиці.
        public string FeaturesString => Features != null ? string.Join(", ", Features) : string.Empty;

        // --- КОНСТРУКТОРИ ---

        // 1. Конструктор за замовчуванням.
        public Gadget()
        {
            Name = string.Empty;
            Manufacturer = string.Empty;
            Price = 0;
            Type = GadgetType.Smartphone; // Значення за замовчуванням
            ScreenSize = string.Empty;
            Features = new List<GadgetFeature>(); // Важливо ініціалізувати список!
            IsNew = true;
        }

        // 2. Конструктор з параметрами.
        public Gadget(string name, string manufacturer, decimal price, GadgetType type, string screenSize, List<GadgetFeature> features)
        {
            Name = name;
            Manufacturer = manufacturer;
            Price = price;
            Type = type;
            ScreenSize = screenSize;
            Features = features ?? new List<GadgetFeature>(); // Перевірка на null
            IsNew = false;
        }

        // 3. Конструктор копіювання.
        public Gadget(Gadget other)
        {
            Name = other.Name;
            Manufacturer = other.Manufacturer;
            Price = other.Price;
            Type = other.Type;
            ScreenSize = other.ScreenSize;
            IsNew = false;
            Features = new List<GadgetFeature>(other.Features); // Створення копії списку
        }
        
        // --- МЕТОДИ ДЛЯ CSV ---

        // Перетворює об'єкт Gadget на рядок у форматі CSV з роздільником ';'.
        public override string ToString()
        {
            // Локальна функція для екранування.
            string Escape(string value)
            {
                if (string.IsNullOrEmpty(value)) return "";
                if (value.Contains(';') || value.Contains('"') || value.Contains('\n'))
                {
                    return $"\"{value.Replace("\"", "\"\"")}\"";
                }
                return value;
            }
            // Об'єднуємо функції через '|'.
            var featuresString = string.Join("|", Features);
            // Формуємо рядок CSV.
            return string.Join(";",
                Escape(Name),
                Escape(Manufacturer),
                Price.ToString(CultureInfo.InvariantCulture), // Гарантуємо '.' як роздільник
                Type.ToString(),
                Escape(ScreenSize),
                Escape(featuresString)
            );
        }

        // Створює об'єкт Gadget з рядка CSV (роздільник ';'). Виконує валідацію форматів.
        public static Gadget Parse(string csvLine)
        {
            var parts = new List<string>();
            var currentPart = new StringBuilder();
            bool inQuotes = false;
            // "Розумний" парсер CSV, що враховує лапки.
            for (int i = 0; i < csvLine.Length; i++)
            {
                char c = csvLine[i];
                if (inQuotes)
                {
                    if (c == '"' && i < csvLine.Length - 1 && csvLine[i + 1] == '"') { currentPart.Append('"'); i++; } 
                    else if (c == '"') { inQuotes = false; } 
                    else { currentPart.Append(c); }
                }
                else
                {
                    if (c == '"') { inQuotes = true; } 
                    else if (c == ';') { parts.Add(currentPart.ToString()); currentPart.Clear(); } 
                    else { currentPart.Append(c); }
                }
            }
            parts.Add(currentPart.ToString());
            // Перевірка кількості полів.
            if (parts.Count != 6) { throw new FormatException($"Рядок має некоректну кількість полів ({parts.Count}, очікується 6)."); }
            
            // Валідація Розміру Екрану.
            string screenSizeString = parts[4];
            if (!string.IsNullOrWhiteSpace(screenSizeString) && !Regex.IsMatch(screenSizeString, @"^\d+[xх]\d+$"))
            {
                throw new FormatException($"Некоректний формат розміру екрану: '{screenSizeString}'. Очікується 'ШиринаxВисота'.");
            }
            
            // Парсинг Списку Функцій.
            var featuresString = parts[5];
            var features = new List<GadgetFeature>();
            if (!string.IsNullOrEmpty(featuresString))
            {
                var featureParts = featuresString.Split('|');
                foreach (var featurePart in featureParts) { if (Enum.TryParse<GadgetFeature>(featurePart, out var feature)) { features.Add(feature); } }
            }
            // Створення об'єкта Gadget.
            return new Gadget(
                name: parts[0], 
                manufacturer: parts[1],
                price: decimal.Parse(parts[2], CultureInfo.InvariantCulture),
                type: Enum.Parse<GadgetType>(parts[3]),
                screenSize: screenSizeString, 
                features: features
            );
        }
    }
}

