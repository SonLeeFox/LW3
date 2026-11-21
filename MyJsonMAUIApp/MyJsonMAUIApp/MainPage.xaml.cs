using System.Text.Json;

namespace StudentsJsonApp;

public partial class MainPage : ContentPage
{
    private readonly StudentCatalog _catalog = new StudentCatalog();
    private StudentFilter _filter = new StudentFilter();

    public MainPage()
    {
        InitializeComponent();
        Refresh();
        StatusLabel.Text = "Відкрийте файл JSON або додайте запис.";
    }

    private void OnFileMenuClicked(object? sender, EventArgs e)
    {
        ShowActionModal("РОБОТА З ФАЙЛОМ JSON", new (string Title, Func<Task> Action)[]
        {
            ("Відкрити JSON-файл із диска…", OpenFileAsync),
            ("Зберегти зміни у поточний файл", () => SaveAsync(false)),
            ("Зберегти як новий JSON-файл…", () => SaveAsync(true))
        });
    }

    private void OnExitClicked(object? sender, EventArgs e)
    {
        string text = _catalog.IsModified
            ? "Є незбережені зміни у списку студентів. Вийти без збереження?"
            : "Завершити роботу з програмою?";

        ShowConfirmModal("ПІДТВЕРДЖЕННЯ ВИХОДУ", text, "Вийти", () =>
        {
            Application.Current?.Quit();
            return Task.CompletedTask;
        });
    }

    private async Task OpenFileAsync()
    {
        if (_catalog.IsModified)
        {
            ShowConfirmModal("НЕЗБЕРЕЖЕНІ ЗМІНИ", "Поточні зміни буде втрачено. Продовжити відкриття іншого файлу?", "Продовжити", PickAndLoadJsonAsync);
            return;
        }

        await PickAndLoadJsonAsync();
    }

    private async Task PickAndLoadJsonAsync()
    {
        try
        {
            FilePickerFileType jsonType = new FilePickerFileType(
                new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.WinUI, new[] { ".json" } }
                });

            FileResult? file = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Оберіть файл JSON",
                FileTypes = jsonType
            });
            if (file == null)
            {
                return;
            }

            await _catalog.OpenAsync(file.FullPath);
            ResetFilter();
            Refresh();
            StatusLabel.Text = "Файл завантажено.";
        }
        catch (Exception ex)
        {
            StatusLabel.Text = DescribeError(ex);
        }
    }

    private async void OnAddClicked(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(new EditPage(null, student =>
        {
            _catalog.Add(student);
            Refresh();
            StatusLabel.Text = "Запис додано.";
        }));
    }

    private async void OnEditClicked(object? sender, EventArgs e)
    {
        Student? selected = GetSelected();
        if (selected == null)
        {
            return;
        }

        await Navigation.PushAsync(new EditPage(selected, updated =>
        {
            _catalog.Replace(selected, updated);
            Refresh();
            StatusLabel.Text = "Запис змінено.";
        }));
    }

    private void OnDeleteClicked(object? sender, EventArgs e)
    {
        Student? selected = GetSelected();
        if (selected == null)
        {
            return;
        }

        ShowConfirmModal("ВИДАЛЕННЯ ЗАПИСУ", $"Видалити запис «{selected.FullName}» ({selected.Subject})?", "Видалити", () =>
        {
            _catalog.Remove(selected);
            Refresh();
            StatusLabel.Text = "Запис видалено.";
            return Task.CompletedTask;
        });
    }

    private async void OnAboutClicked(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(new AboutPage());
    }

    private void OnSearchClicked(object? sender, EventArgs e)
    {
        if (!StudentFilter.TryParseGrade(MinGradeEntry.Text, 0, out int minGrade) ||
            !StudentFilter.TryParseGrade(MaxGradeEntry.Text, 100, out int maxGrade) ||
            minGrade > maxGrade)
        {
            StatusLabel.Text = "Оцінки мають бути цілими числами від 0 до 100, а «від» не більше за «до».";
            return;
        }

        _filter = new StudentFilter
        {
            FullName = Clean(SearchNameEntry),
            Faculty = Clean(SearchFacultyEntry),
            Department = Clean(SearchDepartmentEntry),
            Subject = Clean(SearchSubjectEntry),
            MinGrade = minGrade,
            MaxGrade = maxGrade
        };

        Refresh();
        StatusLabel.Text = "Пошук LINQ виконано.";
    }

    private void OnResetClicked(object? sender, EventArgs e)
    {
        ResetFilter();
        Refresh();
        StatusLabel.Text = "Критерії пошуку очищено.";
    }

    private async Task SaveAsync(bool saveAs)
    {
        try
        {
            string? path = saveAs ? null : _catalog.FilePath;
            if (path == null)
            {
                AskSaveFileNameModal();
                return;
            }

            await _catalog.SaveAsync(path);
            Refresh();
            StatusLabel.Text = "Збережено: " + path;
        }
        catch (Exception ex)
        {
            StatusLabel.Text = DescribeError(ex);
        }
    }

    private static string GetDefaultDataFolder()
    {
        DirectoryInfo? dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !dir.GetFiles("*.csproj").Any())
        {
            dir = dir.Parent;
        }

        string baseFolder = dir?.FullName ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        string dataFolder = Path.Combine(baseFolder, "Data");
        Directory.CreateDirectory(dataFolder);
        return dataFolder;
    }

    private void AskSaveFileNameModal()
    {
        ModalTitleLabel.Text = "ЗБЕРЕГТИ JSON-ФАЙЛ";
        ModalContentPanel.Children.Clear();

        string initialFolder = _catalog.FilePath != null
            ? (Path.GetDirectoryName(_catalog.FilePath) ?? GetDefaultDataFolder())
            : GetDefaultDataFolder();

        ModalContentPanel.Children.Add(new Label
        {
            Text = "Ім'я файлу (.json додасться автоматично):",
            TextColor = Color.FromArgb("#D1D5DB"),
            FontSize = 12
        });

        Entry nameEntry = new Entry
        {
            Text = _catalog.FilePath != null ? Path.GetFileNameWithoutExtension(_catalog.FilePath) : "students",
            TextColor = Color.FromArgb("#F3F4F6"),
            BackgroundColor = Color.FromArgb("#0F1115"),
            FontSize = 12,
            MaxLength = 100
        };

        ModalContentPanel.Children.Add(new Border
        {
            Stroke = Color.FromArgb("#2D333F"),
            StrokeThickness = 1,
            BackgroundColor = Color.FromArgb("#0F1115"),
            HeightRequest = 34,
            Content = nameEntry
        });

        ModalContentPanel.Children.Add(new Label
        {
            Text = "Папка збереження (за замовчуванням — папка Data проєкту):",
            TextColor = Color.FromArgb("#D1D5DB"),
            FontSize = 12,
            Margin = new Thickness(0, 4, 0, 0)
        });

        Entry folderEntry = new Entry
        {
            Text = initialFolder,
            TextColor = Color.FromArgb("#93C5FD"),
            BackgroundColor = Color.FromArgb("#0F1115"),
            FontSize = 11
        };

        ModalContentPanel.Children.Add(new Border
        {
            Stroke = Color.FromArgb("#2D333F"),
            StrokeThickness = 1,
            BackgroundColor = Color.FromArgb("#0F1115"),
            HeightRequest = 34,
            Content = folderEntry
        });

        Button confirmBtn = new Button
        {
            Text = "Зберегти",
            HeightRequest = 32,
            MinimumHeightRequest = 32,
            Margin = new Thickness(0, 4, 0, 0),
            FontSize = 12,
            FontAttributes = FontAttributes.Bold,
            CornerRadius = 0,
            BackgroundColor = Color.FromArgb("#065F46"),
            TextColor = Color.FromArgb("#ECFDF5")
        };

        confirmBtn.Clicked += async (s, e) =>
        {
            string name = (nameEntry.Text ?? "").Trim();
            string folder = (folderEntry.Text ?? "").Trim();

            if (string.IsNullOrEmpty(name) || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                StatusLabel.Text = "Некоректне ім'я файлу.";
                ModalOverlay.IsVisible = false;
                return;
            }

            if (string.IsNullOrEmpty(folder) || folder.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
            {
                StatusLabel.Text = "Некоректний шлях до папки.";
                ModalOverlay.IsVisible = false;
                return;
            }

            if (!name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                name += ".json";
            }

            try
            {
                Directory.CreateDirectory(folder);
            }
            catch (Exception ex)
            {
                StatusLabel.Text = DescribeError(ex);
                ModalOverlay.IsVisible = false;
                return;
            }

            string fullPath = Path.Combine(folder, name);
            ModalOverlay.IsVisible = false;

            if (File.Exists(fullPath))
            {
                ShowConfirmModal("ПІДТВЕРДЖЕННЯ ПЕРЕЗАПИСУ", $"Файл «{name}» уже існує. Перезаписати його?", "Перезаписати", async () =>
                {
                    await ExecuteSaveToPathAsync(fullPath);
                });
                return;
            }

            await ExecuteSaveToPathAsync(fullPath);
        };

        ModalContentPanel.Children.Add(confirmBtn);
        ModalOverlay.IsVisible = true;
    }

    private async Task ExecuteSaveToPathAsync(string path)
    {
        try
        {
            await _catalog.SaveAsync(path);
            Refresh();
            StatusLabel.Text = "Збережено: " + path;
        }
        catch (Exception ex)
        {
            StatusLabel.Text = DescribeError(ex);
        }
    }

    private void ShowActionModal(string title, IEnumerable<(string Title, Func<Task> Action)> items)
    {
        ModalTitleLabel.Text = title;
        ModalContentPanel.Children.Clear();

        foreach (var item in items)
        {
            Button btn = new Button
            {
                Text = item.Title,
                HorizontalOptions = LayoutOptions.Fill,
                HeightRequest = 34,
                MinimumHeightRequest = 34,
                Padding = new Thickness(12, 0),
                FontSize = 12,
                CornerRadius = 0,
                BackgroundColor = Color.FromArgb("#1F242D"),
                TextColor = Color.FromArgb("#F3F4F6"),
                BorderColor = Color.FromArgb("#2D333F"),
                BorderWidth = 1
            };

            btn.Clicked += async (s, e) =>
            {
                ModalOverlay.IsVisible = false;
                await item.Action();
            };

            ModalContentPanel.Children.Add(btn);
        }

        ModalOverlay.IsVisible = true;
    }

    private void ShowConfirmModal(string title, string message, string confirmText, Func<Task> onConfirm)
    {
        ModalTitleLabel.Text = title;
        ModalContentPanel.Children.Clear();

        ModalContentPanel.Children.Add(new Label
        {
            Text = message,
            TextColor = Color.FromArgb("#E5E7EB"),
            FontSize = 12,
            LineHeight = 1.35
        });

        Button confirmBtn = new Button
        {
            Text = confirmText,
            HeightRequest = 32,
            MinimumHeightRequest = 32,
            FontSize = 12,
            FontAttributes = FontAttributes.Bold,
            CornerRadius = 0,
            BackgroundColor = Color.FromArgb("#2563EB"),
            TextColor = Color.FromArgb("#FFFFFF")
        };

        confirmBtn.Clicked += async (s, e) =>
        {
            ModalOverlay.IsVisible = false;
            await onConfirm();
        };

        ModalContentPanel.Children.Add(confirmBtn);
        ModalOverlay.IsVisible = true;
    }

    private void OnCloseModalClicked(object? sender, EventArgs e)
    {
        ModalOverlay.IsVisible = false;
    }

    private Student? GetSelected()
    {
        Student? selected = StudentsView.SelectedItem as Student;
        if (selected == null)
        {
            StatusLabel.Text = "Спочатку оберіть запис у таблиці.";
        }

        return selected;
    }

    private void ResetFilter()
    {
        SearchNameEntry.Text = "";
        SearchFacultyEntry.Text = "";
        SearchDepartmentEntry.Text = "";
        SearchSubjectEntry.Text = "";
        MinGradeEntry.Text = "";
        MaxGradeEntry.Text = "";
        _filter = new StudentFilter();
    }

    private void Refresh()
    {
        List<Student> shown = _catalog.Find(_filter).ToList();
        StudentsView.ItemsSource = shown;

        string file = _catalog.FilePath ?? "новий файл (не збережено)";
        string mark = _catalog.IsModified ? " *" : "";
        FileLabel.Text = $"{file}{mark}";
        MetricsLabel.Text = $"{shown.Count} із {_catalog.All.Count} записів";
    }

    private static string Clean(Entry entry)
    {
        return entry.Text?.Trim() ?? "";
    }

    private static string DescribeError(Exception ex)
    {
        if (ex is JsonException)
        {
            return "Файл має неправильний формат JSON.";
        }
        if (ex is InvalidDataException)
        {
            return ex.Message;
        }
        if (ex is IOException || ex is UnauthorizedAccessException)
        {
            return "Не вдалося прочитати або записати файл.";
        }

        return "Помилка: " + ex.Message;
    }
}