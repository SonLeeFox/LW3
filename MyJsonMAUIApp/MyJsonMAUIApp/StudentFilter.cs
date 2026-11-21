namespace StudentsJsonApp;

// Критерії пошуку. Кожен критерій - окремий Where у запиті LINQ,
// порожній критерій нічого не відсікає, тому їх можна комбінувати.
public class StudentFilter
{
    public string FullName { get; set; } = string.Empty;
    public string Faculty { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public int MinGrade { get; set; }
    public int MaxGrade { get; set; } = 100;

    public IEnumerable<Student> Apply(IEnumerable<Student> students)
    {
        return students
            .Where(s => s.FullName.Contains(FullName, StringComparison.OrdinalIgnoreCase))
            .Where(s => s.Faculty.Contains(Faculty, StringComparison.OrdinalIgnoreCase))
            .Where(s => s.Department.Contains(Department, StringComparison.OrdinalIgnoreCase))
            .Where(s => s.Subject.Contains(Subject, StringComparison.OrdinalIgnoreCase))
            .Where(s => s.Grade >= MinGrade && s.Grade <= MaxGrade);
    }

    // Розбирає межу оцінки з поля вводу. Порожнє поле дає значення за замовчуванням.
    public static bool TryParseGrade(string? text, int defaultValue, out int grade)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            grade = defaultValue;
            return true;
        }

        return int.TryParse(text, out grade) && grade >= 0 && grade <= 100;
    }
}
