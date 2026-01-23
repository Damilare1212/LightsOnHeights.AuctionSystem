using System.Data;
using Microsoft.Data.Sqlite;

var migrationsDir = Path.Combine(AppContext.BaseDirectory, "../../../../migrations");
var dbPath = args.Length > 0 ? args[0] : "./data/app.db";
Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

var files = Directory.GetFiles(migrationsDir, "*.sql").OrderBy(f => f);

using var conn = new SqliteConnection($"Data Source={dbPath}");
conn.Open();

foreach (var file in files)
{
    var sql = await File.ReadAllTextAsync(file);
    using var cmd = conn.CreateCommand();
    cmd.CommandText = sql;
    await cmd.ExecuteNonQueryAsync();
    Console.WriteLine($"Applied migration: {Path.GetFileName(file)}");
}

Console.WriteLine("Migrations applied.");
