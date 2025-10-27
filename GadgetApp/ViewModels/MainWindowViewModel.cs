using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GadgetApp.Exceptions;
using GadgetApp.Models;
using GadgetApp.Services;
using GadgetApp.Views;

namespace GadgetApp.ViewModels
{
    /// ViewModel для головного вікна програми. Це центральний клас, який керує всіма даними та логікою.
    /// Він виступає посередником між "виглядом" (View, .axaml файли) та "даними" (Model, клас Gadget).
    /// Успадковується від ObservableObject для автоматичного сповіщення UI про зміни властивостей.
    public partial class MainWindowViewModel : ObservableObject
    {
        // --- СЕРВІСИ ---
        // Використовуємо Dependency Injection "вручну", створюючи екземпляри сервісів,
        // які інкапсулюють специфічну логіку (робота з файлами, сортування).
        private readonly CsvDataService _dataService = new CsvDataService();

        // --- ДАНІ ---


        // Повний, невідфільтрований список усіх гаджетів.
        // Слугує "копією" або основним джерелом даних.
        // Усі операції (фільтрація, сортування) починаються з цього списку.
        private List<Gadget> _allGadgets;


        // Колекція гаджетів, яка безпосередньо прив'язана до DataGrid в UI.
        // ObservableCollection щоразу при фільтрації/сортуванні, щоб скинути стан DataGrid.

        [ObservableProperty]
        private ObservableCollection<Gadget> _gadgets;

        // Зберігає гаджет, який наразі вибрано у таблиці.
        // Атрибут [NotifyCanExecuteChangedFor] інструмент MVVM Toolkit.
        // Він автоматично викликає перевірку CanExecute для вказаних команд,
        // що дозволяє вмикати/вимикати кнопки "Редагувати" та "Видалити" в залежності від того,
        // чи вибрано якийсь елемент у таблиці.
        
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(EditGadgetCommand))]
        [NotifyCanExecuteChangedFor(nameof(DeleteGadgetCommand))]
        private Gadget? _selectedGadget;

        #region Filter Properties
        // --- ВЛАСТИВОСТІ ДЛЯ ФІЛЬТРАЦІЇ ---
        // Кожна властивість прив'язана до відповідного поля на панелі фільтрів.
        // Завдяки частковим методам On...Changed(), які генерує MVVM Toolkit,
        // фільтрація застосовується миттєво при будь-якій зміні.

        [ObservableProperty] private string? _searchText;
        [ObservableProperty] private string? _selectedManufacturer;
        [ObservableProperty] private GadgetType? _selectedType;
        [ObservableProperty] private string? _minPriceText;
        [ObservableProperty] private string? _maxPriceText;
        [ObservableProperty] private string? _minScreenSize;
        [ObservableProperty] private string? _maxScreenSize;

        // Властивості для відображення тексту помилок валідації під полями фільтрів.
        [ObservableProperty] private string? _minPriceError;
        [ObservableProperty] private string? _maxPriceError;
        [ObservableProperty] private string? _minScreenSizeError;
        [ObservableProperty] private string? _maxScreenSizeError;


        // Колекція унікальних виробників для заповнення випадаючого списку (ComboBox).
        public ObservableCollection<string> Manufacturers { get; } = new();


        /// Колекція всіх можливих типів гаджетів для заповнення випадаючого списку (ComboBox).
        public ObservableCollection<GadgetType> Types { get; } = new();

        #endregion

        #region View Control Properties

        // --- ВЛАСТИВОСТІ ДЛЯ КЕРУВАННЯ ВИГЛЯДОМ ---
        
        // Визначає, чи видима панель фільтрів.
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(MainGridColumnSpan))]
        [NotifyPropertyChangedFor(nameof(ToggleFiltersMenuText))]
        [NotifyPropertyChangedFor(nameof(ShowFilterPanel))]
        private bool _isFilterPanelVisible = true;


        // Перевіряє, чи є в програмі хоча б один гаджет.
        // Використовується для приховування панелі фільтрів, коли немає що фільтрувати.
        public bool HasData => _allGadgets is { Count: > 0 };
        
        /// Комбінована властивість, що визначає, чи повинна панель фільтрів бути видимою.
        /// Панель видима лише тоді, коли вона увімкнена І коли є дані.
        public bool ShowFilterPanel => IsFilterPanelVisible && HasData;


        // Динамічно розраховує, скільки колонок має займати основна сітка (таблиця).
        // 1 - коли панель фільтрів видима, 2 - коли прихована (таблиця розширюється).
        public int MainGridColumnSpan => ShowFilterPanel ? 1 : 2;


        // Динамічно розраховує текст для пункту меню в залежності від стану панелі.
        public string ToggleFiltersMenuText => IsFilterPanelVisible ? "Приховати панель фільтрів" : "Показати панель фільтрів";

        #endregion

        // --- ВЛАСТИВОСТІ ДЛЯ ВІДСТЕЖЕННЯ ЗМІН ---

        // Зберігає шлях до останнього завантаженого або збереженого файлу.
        private string? _currentFilePath;
 
        // Прапорець, що вказує, чи були внесені зміни після останнього збереження/завантаження.
        // Використовується для активації команди "Зберегти" та діалогу при закритті.

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveCommand))] // Команда "Зберегти" залежить від цього
        private bool _isDirty = false;


        // Конструктор ViewModel. Викликається один раз при запуску програми.
        // Ініціалізує початкові дані.

        public MainWindowViewModel()
        {
            _allGadgets = new List<Gadget>();
            // Ініціалізуємо порожньою колекцією
            _gadgets = new ObservableCollection<Gadget>();
            UpdateFilterCollections();
        }
        
        // Перемикає видимість панелі фільтрів.
        [RelayCommand]
        private void ToggleFilterPanel()
        {
            IsFilterPanelVisible = !IsFilterPanelVisible;
        }
        
        // Повертає відфільтровану послідовність гаджетів.
        private IEnumerable<Gadget> GetFilteredGadgets()
        {
            IEnumerable<Gadget> filtered = _allGadgets;

            if (!string.IsNullOrWhiteSpace(SearchText)) { filtered = filtered.Where(g => g.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase)); }
            // Порівняння виробника без урахування регістру 
            if (!string.IsNullOrEmpty(SelectedManufacturer) && SelectedManufacturer != "Всі компанії") { filtered = filtered.Where(g => g.Manufacturer.Equals(SelectedManufacturer, StringComparison.OrdinalIgnoreCase)); }
            if (SelectedType.HasValue) { filtered = filtered.Where(g => g.Type == SelectedType.Value); }
            if (string.IsNullOrEmpty(MinPriceError) && decimal.TryParse(MinPriceText, NumberStyles.Any, CultureInfo.InvariantCulture, out var minPriceValue)) { filtered = filtered.Where(g => g.Price >= minPriceValue); }
            if (string.IsNullOrEmpty(MaxPriceError) && decimal.TryParse(MaxPriceText, NumberStyles.Any, CultureInfo.InvariantCulture, out var maxPriceValue)) { filtered = filtered.Where(g => g.Price <= maxPriceValue); }
            bool TryParseSize(string? text, out (int w, int h) size) { size = (0, 0); if (string.IsNullOrWhiteSpace(text)) return false; var parts = text.Split(new[] { 'x', 'х' }, StringSplitOptions.RemoveEmptyEntries); if (parts.Length == 2 && int.TryParse(parts[0], out var w) && int.TryParse(parts[1], out var h)) { size = (w, h); return true; } return false; }
            if (string.IsNullOrEmpty(MinScreenSizeError) && TryParseSize(MinScreenSize, out var minSize)) { filtered = filtered.Where(g => TryParseSize(g.ScreenSize, out var gadgetSize) && gadgetSize.w >= minSize.w && gadgetSize.h >= minSize.h); }
            if (string.IsNullOrEmpty(MaxScreenSizeError) && TryParseSize(MaxScreenSize, out var maxSize)) { filtered = filtered.Where(g => TryParseSize(g.ScreenSize, out var gadgetSize) && gadgetSize.w <= maxSize.w && gadgetSize.h <= maxSize.h); }

            return filtered;
        }
        
        // Оновлює властивість Gadgets новим екземпляром ObservableCollection.
        private void UpdateVisibleGadgets()
        {
            var filteredData = GetFilteredGadgets();
            Gadgets = new ObservableCollection<Gadget>(filteredData);
        }

        #region Filter Handlers
        // Обробники зміни фільтрів
        partial void OnMinPriceTextChanged(string? value) { MinPriceError = (!string.IsNullOrWhiteSpace(value) && !decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out _)) ? "Має бути число." : null; UpdateVisibleGadgets(); }
        partial void OnMaxPriceTextChanged(string? value) { MaxPriceError = (!string.IsNullOrWhiteSpace(value) && !decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out _)) ? "Має бути число." : null; UpdateVisibleGadgets(); }
        partial void OnMinScreenSizeChanged(string? value) { MinScreenSizeError = (!string.IsNullOrWhiteSpace(value) && !Regex.IsMatch(value, @"^\d+[xх]\d+$")) ? "Формат: 'ШхВ'." : null; UpdateVisibleGadgets(); }
        partial void OnMaxScreenSizeChanged(string? value) { MaxScreenSizeError = (!string.IsNullOrWhiteSpace(value) && !Regex.IsMatch(value, @"^\d+[xх]\d+$")) ? "Формат: 'ШхВ'." : null; UpdateVisibleGadgets(); }
        partial void OnSearchTextChanged(string? value) => UpdateVisibleGadgets();
        partial void OnSelectedManufacturerChanged(string? value) => UpdateVisibleGadgets();
        partial void OnSelectedTypeChanged(GadgetType? value) => UpdateVisibleGadgets();
        #endregion


        // Оновлює колекції для ComboBox'ів фільтрів унікальними значеннями.
        private void UpdateFilterCollections()
        {
            Manufacturers.Clear();
            Manufacturers.Add("Всі компанії");
            // --- Використовуємо Distinct з IEqualityComparer.OrdinalIgnoreCase ---
            // Це гарантує, що "Apple" і "apple" будуть вважатися однаковими.
            var uniqueManufacturers = _allGadgets
                .Select(g => g.Manufacturer)
                .Where(m => !string.IsNullOrWhiteSpace(m)) // Ігноруємо порожні назви
                .Distinct(StringComparer.OrdinalIgnoreCase) 
                .OrderBy(m => m);
            foreach (var man in uniqueManufacturers) { Manufacturers.Add(man); }

            Types.Clear();
            var uniqueTypes = Enum.GetValues(typeof(GadgetType)).Cast<GadgetType>();
            foreach (var type in uniqueTypes) { Types.Add(type); }

            // Зберігаємо поточний вибір, якщо він все ще є в списку, інакше скидаємо
            if (SelectedManufacturer != null && !Manufacturers.Contains(SelectedManufacturer, StringComparer.OrdinalIgnoreCase))
            {
                 SelectedManufacturer = "Всі компанії";
            }
            else if (SelectedManufacturer == null) // Встановлюємо значення за замовчуванням, якщо нічого не було вибрано
            {
                SelectedManufacturer = "Всі компанії";
            }
        }


        #region Main Commands

        // --- КОМАНДИ ---
        
        // Скидає всі фільтри до початкового стану.
        [RelayCommand]
        private void ResetFilters()
        {
            SearchText = null; SelectedManufacturer = "Всі компанії"; SelectedType = null;
            MinPriceText = null; MaxPriceText = null; MinScreenSize = null; MaxScreenSize = null;
            MinPriceError = null; MaxPriceError = null; MinScreenSizeError = null; MaxScreenSizeError = null;
            UpdateVisibleGadgets();
        }
        
        // Сортує список гаджетів за ціною за допомогою Quick Sort.
        [RelayCommand]
        private void SortByPrice()
        {
            if (_allGadgets is { Count: > 1 })
            {
                SortingService.QuickSortByPrice(_allGadgets);
                IsDirty = true;
                UpdateVisibleGadgets();
            }
        }

        // Відкриває вікно для додавання нового гаджета.
        [RelayCommand]
        private async Task AddGadget()
        {
            var newGadget = new Gadget();
            var viewModel = new AddEditGadgetViewModel(newGadget);
            var dialog = new AddEditGadgetWindow { DataContext = viewModel };
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
            {
                var result = await dialog.ShowDialog<bool>(desktop.MainWindow);
                if (result)
                {
                    viewModel.UpdateGadget(newGadget);
                    _allGadgets.Add(newGadget);
                    IsDirty = true;
                    OnPropertyChanged(nameof(HasData)); OnPropertyChanged(nameof(ShowFilterPanel)); OnPropertyChanged(nameof(MainGridColumnSpan));
                    UpdateVisibleGadgets();
                    UpdateFilterCollections(); 
                }
            }
        }

        private bool CanEditGadget() => SelectedGadget is not null;
        
        // Відкриває вікно для редагування обраного гаджета.

        [RelayCommand(CanExecute = nameof(CanEditGadget))]
        private async Task EditGadget()
        {
            var gadgetToEditCopy = new Gadget(SelectedGadget!);
            var viewModel = new AddEditGadgetViewModel(gadgetToEditCopy);
            var dialog = new AddEditGadgetWindow { DataContext = viewModel };
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
            {
                var result = await dialog.ShowDialog<bool>(desktop.MainWindow);
                if (result)
                {
                    viewModel.UpdateGadget(gadgetToEditCopy);
                    var index = _allGadgets.IndexOf(SelectedGadget!);
                    if (index != -1)
                    {
                        _allGadgets[index] = gadgetToEditCopy;
                        IsDirty = true;
                        UpdateVisibleGadgets();
                        UpdateFilterCollections(); 
                    }
                }
            }
        }

        private bool CanDeleteGadget() => SelectedGadget is not null;

        // Видаляє обраний гаджет після підтвердження користувачем.
        [RelayCommand(CanExecute = nameof(CanDeleteGadget))]
        private async Task DeleteGadget()
        {
            if (SelectedGadget is null || Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop || desktop.MainWindow is null) return;
            if (await ConfirmDialog.ShowAsync(desktop.MainWindow, "Ви впевнені, що хочете видалити цей гаджет?"))
            {
                _allGadgets.Remove(SelectedGadget);
                IsDirty = true;
                OnPropertyChanged(nameof(HasData)); OnPropertyChanged(nameof(ShowFilterPanel)); OnPropertyChanged(nameof(MainGridColumnSpan));
                UpdateVisibleGadgets();
                UpdateFilterCollections(); 
            }
        }

        private bool CanSave() => IsDirty && !string.IsNullOrEmpty(_currentFilePath);

        // Зберігає зміни в поточний відкритий файл.

        [RelayCommand(CanExecute = nameof(CanSave))]
        private void Save()
        {
            if (string.IsNullOrEmpty(_currentFilePath) || Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop || desktop.MainWindow is null) return;
            try
            {
                _dataService.Save(_allGadgets, _currentFilePath);
                IsDirty = false;
            }
            catch (UnauthorizedAccessException) { MessageBoxView.Show(desktop.MainWindow, "Помилка доступу", $"Немає дозволу на запис у файл:\n{_currentFilePath}"); }
            catch (IOException ex) { MessageBoxView.Show(desktop.MainWindow, "Помилка збереження", $"Не вдалося зберегти файл.\n\nДеталі: {ex.Message}"); }
        }
        
        // Зберігає список гаджетів у новий файл (Зберегти як...).
        [RelayCommand]
        private async Task SaveAs()
        {
             if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop || desktop.MainWindow is null) return;
            var dialog = new SaveFileDialog { Title = "Зберегти список гаджетів як...", Filters = new List<FileDialogFilter> { new() { Name = "CSV Files", Extensions = { "csv" } } }, InitialFileName = "gadgets.csv" };
            var filePath = await dialog.ShowAsync(desktop.MainWindow);
            if (!string.IsNullOrEmpty(filePath))
            {
                try
                {
                    _dataService.Save(_allGadgets, filePath);
                    _currentFilePath = filePath;
                    IsDirty = false;
                }
                catch (UnauthorizedAccessException) { MessageBoxView.Show(desktop.MainWindow, "Помилка доступу", $"Немає дозволу на запис у файл:\n{filePath}"); }
                catch (IOException ex) { MessageBoxView.Show(desktop.MainWindow, "Помилка збереження", $"Не вдалося зберегти файл.\n\nДеталі: {ex.Message}"); }
            }
        }
        
        // Завантажує список гаджетів з файлу, попередньо запитавши про збереження змін.
        [RelayCommand]
        private async Task LoadFromFile()
        {
            // Перевірка незбережених змін
            if (IsDirty)
            {
                if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop || desktop.MainWindow is null) return;
                var result = await SaveChangesDialog.ShowAsync(desktop.MainWindow);
                switch (result)
                {
                    case SaveChangesResult.Save: if (CanSave()) Save(); else await SaveAs(); if (IsDirty) return; break;
                    case SaveChangesResult.DontSave: break;
                    case SaveChangesResult.Cancel: return;
                }
            }

            // Процес завантаження
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop2 || desktop2.MainWindow is null) return;
            var dialog = new OpenFileDialog { Title = "Завантажити список гаджетів", Filters = new List<FileDialogFilter> { new() { Name = "CSV Files", Extensions = { "csv" } } }, AllowMultiple = false };
            var files = await dialog.ShowAsync(desktop2.MainWindow);
            var filePath = files?.FirstOrDefault();
            if (!string.IsNullOrEmpty(filePath))
            {
                try
                {
                    _allGadgets = _dataService.Load(filePath);
                    _currentFilePath = filePath;
                    IsDirty = false;
                    OnPropertyChanged(nameof(HasData)); OnPropertyChanged(nameof(ShowFilterPanel)); OnPropertyChanged(nameof(MainGridColumnSpan));
                    ResetFilters();
                    UpdateFilterCollections(); 
                }
                catch (FileNotFoundException) { MessageBoxView.Show(desktop2.MainWindow, "Файл не знайдено", $"Файл за шляхом '{filePath}' не існує."); }
                catch (CsvParsingException ex) { string msg = $"Помилка в рядку: {ex.LineNumber}\nВміст: \"{ex.LineContent}\"\nДеталі: {ex.InnerException?.Message}"; MessageBoxView.Show(desktop2.MainWindow, "Помилка формату файлу", msg); }
                catch (IOException ex) { MessageBoxView.Show(desktop2.MainWindow, "Помилка читання файлу", $"Не вдалося прочитати файл.\n\nДеталі: {ex.Message}"); }
                catch (Exception ex) { MessageBoxView.Show(desktop2.MainWindow, "Невідома помилка", $"Сталася непередбачувана помилка:\n\n{ex.Message}"); }
            }
        }
        #endregion
        
        // Асинхронний метод, який перевіряє незбережені зміни перед закриттям програми.
        public async Task<bool> CanCloseAsync()
        {
            if (!IsDirty) return true;
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop || desktop.MainWindow is null) return true;
            var result = await SaveChangesDialog.ShowAsync(desktop.MainWindow);
            switch (result)
            {
                case SaveChangesResult.Save: if (CanSave()) Save(); else await SaveAs(); return !IsDirty; 
                case SaveChangesResult.DontSave: return true;
                case SaveChangesResult.Cancel: default: return false;
            }
        }
    }
}

