using CommunityToolkit.Mvvm.ComponentModel;
using GadgetApp.Models;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace GadgetApp.ViewModels
{

    // ViewModel для вікна додавання та редагування гаджета.
    // Містить логіку для валідації введених даних при збереженні.
    public partial class AddEditGadgetViewModel : ObservableObject
    {
        // --- ВЛАСТИВОСТІ, ПРИВ'ЯЗАНІ ДО UI ---
       
        // Прапорець для функції "Камера".
        [ObservableProperty] private bool _hasCamera;
        
        // Прапорець для функції "GPS".
        [ObservableProperty] private bool _hasGPS;
        
        // Прапорець для функції "Вимірювач пульсу".
        [ObservableProperty] private bool _hasPulseMeter;
        
        // Назва гаджета, введена користувачем.
        [ObservableProperty] private string _name = "";
        
        // Виробник гаджета, введений користувачем.
        [ObservableProperty] private string _manufacturer = "";

        // Тип гаджета, обраний користувачем.
        [ObservableProperty] private GadgetType _type;

        // Розмір екрану, введений користувачем.
        [ObservableProperty] private string _screenSize = "";

        // --- ВЛАСТИВОСТІ ДЛЯ ВІДОБРАЖЕННЯ ПОМИЛОК ---
        // Ці властивості оновлюються методом Validate() і прив'язані до TextBlock в UI.
        
        // Текст помилки для поля "Назва".
        [ObservableProperty] private string? _nameError;

        // Текст помилки для поля "Виробник".
        [ObservableProperty] private string? _manufacturerError;
   
        // Текст помилки для поля "Ціна".
        [ObservableProperty] private string? _priceError;
   
        // Текст помилки для поля "Розмір екрану".
        [ObservableProperty] private string? _screenSizeError;


        // Внутрішнє поле для зберігання числового значення ціни.
        private decimal _priceValue;

        // Текстове представлення ціни для поля вводу.
        // Дозволяє користувачу вводити текст, але зберігає числове значення всередині.
        // Автоматично форматує вивід з двома знаками після коми.
        public string? PriceText
        {
            get => _priceValue == 0 ? "" : _priceValue.ToString("F2", CultureInfo.InvariantCulture);
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    _priceValue = 0; // Вважаємо порожній рядок нулем
                }
                // Намагаємося перетворити текст у число, ігноруючи помилки введення
                else if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
                {
                    _priceValue = parsed; // Зберігаємо успішно розпізнане число
                }
                // Якщо TryParse не вдався (введено нечислові символи), _priceValue не змінюється,
                // але OnPropertyChanged все одно викликається, щоб UI міг оновити текст (напр., видалити букви).
                // Остаточна валідація відбудеться в методі Validate().

                // Повідомляємо UI, що значення (текстове) могло змінитися (для форматування .00)
                OnPropertyChanged(nameof(PriceText));
            }
        }


        // Заголовок вікна ("Додати новий гаджет" або "Редагувати гаджет").
        public string Title { get; }


        // Список типів гаджетів для заповнення ComboBox.
        public List<GadgetType> GadgetTypes { get; } = System.Enum.GetValues(typeof(GadgetType)).Cast<GadgetType>().ToList();

       
        // Конструктор ViewModel.
        public AddEditGadgetViewModel(Gadget gadget)
        {
            // Копіюємо дані з моделі у властивості ViewModel при ініціалізації
            _name = gadget.Name;
            _manufacturer = gadget.Manufacturer;
            _priceValue = gadget.Price;
            _type = gadget.Type;
            _screenSize = gadget.ScreenSize;
            _hasCamera = gadget.Features.Contains(GadgetFeature.Camera);
            _hasGPS = gadget.Features.Contains(GadgetFeature.GPS);
            _hasPulseMeter = gadget.Features.Contains(GadgetFeature.PulseMeter);
            Title = gadget.IsNew ? "Додати новий гаджет" : "Редагувати гаджет";
            // Початкова валідація не проводиться, помилки з'являться лише після спроби збереження.
        }
        
        // Метод для валідації всіх полів ViewModel. Викликається вручну перед збереженням.
        // Перевіряє кожне поле на відповідність критеріям (порожнє, довжина, діапазон, формат).
        // Оновлює властивості ...Error, які відображаються в UI.
        public bool Validate()
        {
            // Скидаємо прапорець валідності перед кожною перевіркою
            bool isValid = true;

            // Валідація Назви
            if (string.IsNullOrWhiteSpace(Name)) { NameError = "Назва не може бути порожньою."; isValid = false; }
            else if (Name.Length > 50) { NameError = "Назва занадто довга (макс. 50 символів)."; isValid = false; }
            else NameError = null; // Скидаємо помилку, якщо поле валідне

            // Валідація Виробника
            if (string.IsNullOrWhiteSpace(Manufacturer)) { ManufacturerError = "Виробник не може бути порожнім."; isValid = false; }
            // Можна додати перевірку довжини, якщо потрібно
            // else if (Manufacturer.Length > 50) { ManufacturerError = "Назва виробника занадто довга."; isValid = false; }
            else ManufacturerError = null;

            // Валідація Ціни (використовуємо внутрішнє числове значення _priceValue)
            if (_priceValue <= 0 || _priceValue > 100000) { PriceError = "Ціна має бути в діапазоні від 0.01 до 100000."; isValid = false; }
            else PriceError = null;

            // Валідація Розміру екрану
            if (string.IsNullOrWhiteSpace(ScreenSize)) { ScreenSizeError = "Вкажіть розмір екрану."; isValid = false; }
            // Використовуємо регулярний вираз для перевірки формату "Число x Число"
            else if (!Regex.IsMatch(ScreenSize, @"^\d+[xх]\d+$")) { ScreenSizeError = "Невірний формат. Введіть у вигляді 'ШиринаxВисота'."; isValid = false; }
            else ScreenSizeError = null;

            return isValid; // Повертаємо загальний результат валідації
        }
        
        // Оновлює переданий об'єкт моделі `Gadget` поточними даними з властивостей ViewModel.
        // Викликається після успішної валідації перед закриттям вікна.
        public void UpdateGadget(Gadget gadget)
        {
            gadget.Name = this.Name;
            // Автоматично форматуємо назву виробника (перша літера велика)
            gadget.Manufacturer = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(this.Manufacturer.ToLower());
            gadget.Price = _priceValue; // Використовуємо внутрішнє числове значення
            gadget.Type = this.Type;
            gadget.ScreenSize = this.ScreenSize;

            // Оновлюємо список додаткових функцій на основі стану прапорців
            gadget.Features.Clear();
            if (HasCamera) gadget.Features.Add(GadgetFeature.Camera);
            if (HasGPS) gadget.Features.Add(GadgetFeature.GPS);
            if (HasPulseMeter) gadget.Features.Add(GadgetFeature.PulseMeter);
        }
    }
}

