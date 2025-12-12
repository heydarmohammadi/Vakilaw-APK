using AsyncAwaitBestPractices;
using Microsoft.Data.Sqlite;

namespace Vakilaw.Services;

public class DatabaseService
{
    private readonly string _dbPath;
    private readonly string _connectionString;

    public DatabaseService(string dbPath)
    {
        _dbPath = dbPath;

        if (!File.Exists(_dbPath))
            using (File.Create(_dbPath)) { }

        _connectionString = $"Data Source={_dbPath};Mode=ReadWriteCreate;Cache=Shared";

        InitializeDatabase().SafeFireAndForget();
    }

    private async Task InitializeDatabase()
    {
        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();

        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
        CREATE TABLE IF NOT EXISTS Users (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            FullName TEXT NOT NULL,
            PhoneNumber TEXT NOT NULL UNIQUE,
            Role TEXT NOT NULL,
            LicenseNumber TEXT
        );

        CREATE TABLE IF NOT EXISTS Lawyers (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            FullName TEXT NOT NULL,
            PhoneNumber TEXT,
            City TEXT,
            Address TEXT
        );

        CREATE TABLE IF NOT EXISTS Laws (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            ArticleNumber INTEGER,
            LawType TEXT,
            Title TEXT,
            Text TEXT,
            Notes TEXT,
            IsBookmarked INTEGER,
            IsExpanded INTEGER
        );

        CREATE TABLE IF NOT EXISTS Licenses (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            DeviceId TEXT,
            LicenseKey TEXT,
            UserPhone TEXT,
            StartDate INTEGER NOT NULL,
            EndDate INTEGER NOT NULL,
            IsActive INTEGER NOT NULL,
            SubscriptionType TEXT
        );

        CREATE TABLE IF NOT EXISTS Clients (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            FullName TEXT NOT NULL,
            NationalCode TEXT,
            PhoneNumber TEXT,
            Address TEXT,
            Description TEXT
        );

        CREATE TABLE IF NOT EXISTS Cases (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Title TEXT NOT NULL,
            CaseNumber TEXT,
            CourtName TEXT,
            JudgeName TEXT,
            StartDate TEXT,
            EndDate TEXT,
            Status TEXT,
            Description TEXT,
            ClientId INTEGER NOT NULL,
            FOREIGN KEY (ClientId) REFERENCES Clients(Id) ON DELETE CASCADE
        );

        CREATE TABLE IF NOT EXISTS CaseAttachments (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            CaseId INTEGER NOT NULL,
            FileName TEXT NOT NULL,
            FilePath TEXT NOT NULL,
            FileType TEXT,
            FOREIGN KEY (CaseId) REFERENCES Cases(Id) ON DELETE CASCADE
        );

        CREATE TABLE IF NOT EXISTS SmsHistory (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            ClientName TEXT NOT NULL,
            PhoneNumber TEXT NOT NULL,
            Message TEXT NOT NULL,
            SetDate TEXT NOT NULL,
            StatusText TEXT NOT NULL,
            IsGroup INTEGER NOT NULL
        );

        CREATE TABLE IF NOT EXISTS Transactions (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Title TEXT NOT NULL,
            Amount REAL NOT NULL,
            IsIncome INTEGER NOT NULL,
            Date TEXT NOT NULL,
            Description TEXT
        );

        CREATE TABLE IF NOT EXISTS Reminders (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Title TEXT NOT NULL,
                        Description TEXT NOT NULL,
                        Category TEXT NOT NULL,                       
                        ReminderDate TEXT,       
                        IsReminderSet TEXT,
                        IsReminderDone TEXT,
                        CreatedAt TEXT NOT NULL
        );

        CREATE INDEX IF NOT EXISTS idx_clients_fullname ON Clients (FullName);
    ";
        await cmd.ExecuteNonQueryAsync();
    }

    public SqliteConnection GetConnection() => new SqliteConnection(_connectionString);
}