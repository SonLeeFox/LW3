namespace StudentsJsonApp;

public partial class EditPage : ContentPage
{
    private readonly Action<Student> _onSaved;

    public EditPage(Student? source, Action<Student> onSaved)
    {
        InitializeComponent();
        _onSaved = onSaved;
        HeaderTitleLabel.Text = source == null ? "НОВИЙ ЗАПИС ПРО УСПІШНІСТЬ" : "РЕДАГУВАННЯ ЗАПИСУ";

        if (source == null)
        {
            return;
        }

        NameEntry.Text = source.FullName;
        FacultyEntry.Text = source.Faculty;
        DepartmentEntry.Text = source.Department;
        CourseEntry.Text = source.Course.ToString();
        SubjectEntry.Text = source.Subject;
        GradeEntry.Text = source.Grade.ToString();
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        Student student = new Student
        {
            FullName = Clean(NameEntry),
            Faculty = Clean(FacultyEntry),
            Department = Clean(DepartmentEntry),
            Course = ParseOrInvalid(CourseEntry.Text),
            Subject = Clean(SubjectEntry),
            Grade = ParseOrInvalid(GradeEntry.Text)
        };

        string? error = student.Validate();
        if (error != null)
        {
            ErrorLabel.Text = error;
            return;
        }

        _onSaved(student);
        await Navigation.PopAsync();
    }

    private async void OnCancelClicked(object? sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private static string Clean(Entry entry)
    {
        return entry.Text?.Trim() ?? "";
    }

    private static int ParseOrInvalid(string? text)
    {
        return int.TryParse(text, out int value) ? value : -1;
    }
}
