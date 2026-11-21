namespace StudentsJsonApp;

// Запис про успішність студента: один рядок таблиці та один об'єкт у файлі JSON.
public class Student
{
    public string FullName { get; set; } = string.Empty;
    public string Faculty { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public int Course { get; set; }
    public string Subject { get; set; } = string.Empty;
    public int Grade { get; set; }

    // Перевіряє дані запису. Повертає текст помилки або null, якщо все гаразд.
    public string? Validate()
    {
        if (string.IsNullOrWhiteSpace(FullName))
        {
            return "Не вказано ПІБ студента.";
        }
        if (string.IsNullOrWhiteSpace(Faculty))
        {
            return "Не вказано факультет.";
        }
        if (string.IsNullOrWhiteSpace(Department))
        {
            return "Не вказано кафедру.";
        }
        if (Course < 1 || Course > 6)
        {
            return "Курс має бути цілим числом від 1 до 6.";
        }
        if (string.IsNullOrWhiteSpace(Subject))
        {
            return "Не вказано назву дисципліни.";
        }
        if (Grade < 0 || Grade > 100)
        {
            return "Оцінка має бути цілим числом від 0 до 100.";
        }

        return null;
    }
}
