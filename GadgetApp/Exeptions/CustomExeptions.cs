using System;

namespace GadgetApp.Exceptions
{

    // Спеціальний виняток, що виникає при помилці парсингу рядка CSV.
    // Він містить додаткову інформацію про місце виникнення помилки.
    public class CsvParsingException : Exception
    {
        
        // Номер рядка у файлі, де сталася помилка.
        public int LineNumber { get; }
        
        // Вміст рядка, який не вдалося розпарсити.
        public string LineContent { get; }

        public CsvParsingException(string message, int lineNumber, string lineContent, Exception innerException)
            : base(message, innerException)
        {
            LineNumber = lineNumber;
            LineContent = lineContent;
        }
    }
}