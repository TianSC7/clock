using System.IO;
using Microsoft.Data.Sqlite;
using PersonalAssistant.Models;

namespace PersonalAssistant.Core;

public class DatabaseService
{
    private static readonly string DbPath = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "PersonalAssistant", "data.db");

    private static readonly string ConnectionString = $"Data Source={DbPath}";

    public void Initialize()
    {
        var dir = System.IO.Path.GetDirectoryName(DbPath)!;
        Directory.CreateDirectory(dir);

        using var conn = new SqliteConnection(ConnectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS todos (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                title TEXT NOT NULL,
                priority INTEGER DEFAULT 1,
                due_date TEXT,
                is_done INTEGER DEFAULT 0,
                created_at TEXT NOT NULL,
                done_at TEXT
            );

            CREATE TABLE IF NOT EXISTS logs (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                date TEXT NOT NULL,
                time TEXT NOT NULL,
                content TEXT NOT NULL,
                source TEXT DEFAULT 'manual'
            );

            CREATE TABLE IF NOT EXISTS sessions (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                start_at TEXT,
                end_at TEXT,
                phase TEXT,
                completed INTEGER DEFAULT 0
            );
        ";
        cmd.ExecuteNonQuery();
    }

    // === Todo ===

    public List<TodoItem> GetAllTodos()
    {
        var list = new List<TodoItem>();
        using var conn = new SqliteConnection(ConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM todos ORDER BY is_done ASC, priority DESC, created_at DESC";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new TodoItem
            {
                Id = reader.GetInt32(0),
                Title = reader.GetString(1),
                Priority = reader.GetInt32(2),
                DueDate = reader.IsDBNull(3) ? null : reader.GetString(3),
                IsDone = reader.GetInt32(4) == 1,
                CreatedAt = reader.GetString(5),
                DoneAt = reader.IsDBNull(6) ? null : reader.GetString(6)
            });
        }
        return list;
    }

    public void AddTodo(TodoItem item)
    {
        using var conn = new SqliteConnection(ConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO todos (title, priority, due_date, created_at) VALUES (@title, @priority, @due_date, @created_at)";
        cmd.Parameters.AddWithValue("@title", item.Title);
        cmd.Parameters.AddWithValue("@priority", item.Priority);
        cmd.Parameters.AddWithValue("@due_date", (object?)item.DueDate ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@created_at", item.CreatedAt);
        cmd.ExecuteNonQuery();
    }

    public void UpdateTodo(TodoItem item)
    {
        using var conn = new SqliteConnection(ConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE todos SET title=@title, priority=@priority, due_date=@due_date, is_done=@is_done, done_at=@done_at WHERE id=@id";
        cmd.Parameters.AddWithValue("@id", item.Id);
        cmd.Parameters.AddWithValue("@title", item.Title);
        cmd.Parameters.AddWithValue("@priority", item.Priority);
        cmd.Parameters.AddWithValue("@due_date", (object?)item.DueDate ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@is_done", item.IsDone ? 1 : 0);
        cmd.Parameters.AddWithValue("@done_at", (object?)item.DoneAt ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    public void DeleteTodo(int id)
    {
        using var conn = new SqliteConnection(ConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM todos WHERE id=@id";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
    }

    // === Logs ===

    public List<LogEntry> GetLogsByDate(string date)
    {
        var list = new List<LogEntry>();
        using var conn = new SqliteConnection(ConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM logs WHERE date=@date ORDER BY time ASC";
        cmd.Parameters.AddWithValue("@date", date);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new LogEntry
            {
                Id = reader.GetInt32(0),
                Date = reader.GetString(1),
                Time = reader.GetString(2),
                Content = reader.GetString(3),
                Source = reader.IsDBNull(4) ? "manual" : reader.GetString(4)
            });
        }
        return list;
    }

    public List<string> GetLogDates()
    {
        var list = new List<string>();
        using var conn = new SqliteConnection(ConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT DISTINCT date FROM logs ORDER BY date DESC";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(reader.GetString(0));
        }
        return list;
    }

    public void AddLog(string content, string source = "manual")
    {
        var now = DateTime.Now;
        using var conn = new SqliteConnection(ConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO logs (date, time, content, source) VALUES (@date, @time, @content, @source)";
        cmd.Parameters.AddWithValue("@date", now.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("@time", now.ToString("HH:mm:ss"));
        cmd.Parameters.AddWithValue("@content", content);
        cmd.Parameters.AddWithValue("@source", source);
        cmd.ExecuteNonQuery();
    }

    public List<LogEntry> GetAllLogs()
    {
        var list = new List<LogEntry>();
        using var conn = new SqliteConnection(ConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM logs ORDER BY date DESC, time ASC";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new LogEntry
            {
                Id = reader.GetInt32(0),
                Date = reader.GetString(1),
                Time = reader.GetString(2),
                Content = reader.GetString(3),
                Source = reader.IsDBNull(4) ? "manual" : reader.GetString(4)
            });
        }
        return list;
    }

    // === Sessions ===

    public void AddSession(string startAt, string? endAt, string phase, bool completed)
    {
        using var conn = new SqliteConnection(ConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO sessions (start_at, end_at, phase, completed) VALUES (@start_at, @end_at, @phase, @completed)";
        cmd.Parameters.AddWithValue("@start_at", startAt);
        cmd.Parameters.AddWithValue("@end_at", (object?)endAt ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@phase", phase);
        cmd.Parameters.AddWithValue("@completed", completed ? 1 : 0);
        cmd.ExecuteNonQuery();
    }

    public int GetCompletedFocusCountToday()
    {
        using var conn = new SqliteConnection(ConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM sessions WHERE phase='focus' AND completed=1 AND date(start_at)=@today";
        cmd.Parameters.AddWithValue("@today", DateTime.Now.ToString("yyyy-MM-dd"));
        var result = cmd.ExecuteScalar();
        return result != null ? Convert.ToInt32(result) : 0;
    }
}
