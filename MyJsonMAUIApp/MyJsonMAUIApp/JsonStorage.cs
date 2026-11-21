using System.Text.Encodings.Web;
using System.Text.Json;

namespace StudentsJsonApp;

// Робота з файлом JSON: серіалізація та десеріалізація списку записів.
public static class JsonStorage
{
    private const int BufferSize = 4096;

    private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        // щоб українські літери записувались у файл як є, а не у вигляді \u0414
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    // Десеріалізує файл у список записів і перевіряє кожен запис
    public static async Task<List<Student>> LoadAsync(string path)
    {
        List<Student>? students;
        using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, FileOptions.Asynchronous))
        {
            students = await JsonSerializer.DeserializeAsync<List<Student>>(stream, Options);
        }

        if (students == null)
        {
            throw new InvalidDataException("Файл не містить списку записів.");
        }

        for (int i = 0; i < students.Count; i++)
        {
            string? error = students[i] == null ? "порожнє значення." : students[i].Validate();
            if (error != null)
            {
                throw new InvalidDataException($"Запис №{i + 1}: {error}");
            }
        }

        return students;
    }

    // Серіалізує список у JSON. Спочатку пише у тимчасовий файл, щоб збій не пошкодив основний.
    public static async Task SaveAsync(string path, List<Student> students)
    {
        string tempPath = path + ".tmp";
        using (FileStream stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, FileOptions.Asynchronous))
        {
            await JsonSerializer.SerializeAsync(stream, students, Options);
        }

        File.Move(tempPath, path, true);
    }
}
