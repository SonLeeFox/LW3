namespace StudentsJsonApp;

// Список записів і стан файлу. Уся логіка даних тут, щоб обробники кнопок залишались простими.
public class StudentCatalog
{
    private readonly List<Student> _students = new List<Student>();

    public List<Student> All
    {
        get { return _students; }
    }

    // Шлях до поточного файлу (null, якщо список ще не збережено)
    public string? FilePath { get; private set; }

    // true, якщо є зміни, яких немає у файлі
    public bool IsModified { get; private set; }

    public async Task OpenAsync(string path)
    {
        List<Student> loaded = await JsonStorage.LoadAsync(path);

        _students.Clear();
        _students.AddRange(loaded);
        FilePath = path;
        IsModified = false;
    }

    public async Task SaveAsync(string path)
    {
        await JsonStorage.SaveAsync(path, _students);

        FilePath = path;
        IsModified = false;
    }

    public void Add(Student student)
    {
        _students.Add(student);
        IsModified = true;
    }

    public void Replace(Student oldStudent, Student newStudent)
    {
        int index = _students.IndexOf(oldStudent);
        if (index < 0)
        {
            return;
        }

        _students[index] = newStudent;
        IsModified = true;
    }

    public void Remove(Student student)
    {
        if (_students.Remove(student))
        {
            IsModified = true;
        }
    }

    public IEnumerable<Student> Find(StudentFilter filter)
    {
        return filter.Apply(_students);
    }
}
