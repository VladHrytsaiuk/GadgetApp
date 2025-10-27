
using GadgetApp.Exceptions;
using GadgetApp.Models;
using System;
using System.Collections.Generic;
using System.IO;

namespace GadgetApp.Services
{
    // Сервісний клас, що інкапсулює логіку читання та запису даних у форматі .csv.
    public class CsvDataService
    {

        // Зберігає колекцію гаджетів у файл за вказаним шляхом.
        public void Save(IEnumerable<Gadget> gadgets, string filePath)
        {
            // 'using' гарантує, що StreamWriter буде коректно закритий навіть у разі помилки.
            using (var writer = new StreamWriter(filePath))
            {
                foreach (var gadget in gadgets)
                {
                    writer.WriteLine(gadget.ToString());
                }
            }
        }
        
        // Завантажує список гаджетів з файлу.
        public List<Gadget> Load(string filePath)
        {
            var gadgets = new List<Gadget>();
            int lineNumber = 0;
            
            using (var reader = new StreamReader(filePath))
            {
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    lineNumber++;
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    try
                    {
                        var gadget = Gadget.Parse(line);
                        gadgets.Add(gadget);
                    }
                    catch (FormatException ex)
                    {
                        // Створюємо наш власний, більш детальний виняток
                        throw new CsvParsingException(
                            message: $"Некоректний формат даних у рядку.",
                            lineNumber: lineNumber,
                            lineContent: line,
                            innerException: ex
                        );
                    }
                }
            }
            return gadgets;
        }
    }
}