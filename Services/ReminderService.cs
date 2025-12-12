using Microsoft.Data.Sqlite;
using System.Globalization;
using Vakilaw.Models;

namespace Vakilaw.Services
{
    public class ReminderService
    {
        private readonly DatabaseService _dbService;
        public ReminderService(DatabaseService dbService)
        {
            _dbService = dbService;
        }    

        public async Task<List<ReminderModel>> GetNotesPagedAsync(int pageNumber, int pageSize)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;

            int offset = (pageNumber - 1) * pageSize;
            var notes = new List<ReminderModel>();

            using var connection = _dbService.GetConnection();
            connection.Open();
        
            var command = connection.CreateCommand();

            command.CommandText = @"
            SELECT Id, Title, Description, Category, ReminderDate, IsReminderSet, IsReminderDone, CreatedAt
            FROM Reminders
            ORDER BY CreatedAt DESC
            LIMIT @PageSize OFFSET @Offset;
        ";
            command.Parameters.AddWithValue("@PageSize", pageSize);
            command.Parameters.AddWithValue("@Offset", offset);

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                notes.Add(ReadNote(reader));
            }

            return notes;
        }

        public async Task<int> AddNoteAsync(ReminderModel note)
        {
            if (note == null) throw new ArgumentNullException(nameof(note));

            using var connection = _dbService.GetConnection();
            connection.Open();
        
            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO Reminders (Title, Description, Category, ReminderDate, IsReminderSet, IsReminderDone, CreatedAt)
                VALUES (@Title, @Description, @Category, @ReminderDate, @IsReminderSet, @IsReminderDone, @CreatedAt);
                SELECT last_insert_rowid();
            ";

            command.Parameters.AddWithValue("@Title", note.Title);
            command.Parameters.AddWithValue("@Description", note.Description);
            command.Parameters.AddWithValue("@Category", note.Category);           
            command.Parameters.AddWithValue("@ReminderDate", note.ReminderDate.HasValue ? (object)note.ReminderDate.Value : DBNull.Value);
            command.Parameters.AddWithValue("@IsReminderSet", note.IsReminderSet);
            command.Parameters.AddWithValue("@IsReminderDone", note.IsReminderDone);
            command.Parameters.AddWithValue("@CreatedAt", note.CreatedAt);

            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result); // ID جدید نوت
        }

        public async Task<List<ReminderModel>> GetNotesWithFutureRemindersAsync()
        {
            using var connection = _dbService.GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
            SELECT Id, Title, Description, Category, ReminderDate, IsReminderSet, IsReminderDone, CreatedAt
            FROM Reminders
            WHERE 
            ReminderDate IS NOT NULL 
            AND ReminderDate > @Now
            AND IsReminderSet = 1
            AND IsReminderDone = 0
            ";
            command.Parameters.AddWithValue("@Now", DateTime.Now.ToLocalTime());

            var notes = new List<ReminderModel>();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var note = new ReminderModel
                {
                    Id = reader.GetInt32(0),
                    Title = reader.GetString(1),
                    Description = reader.GetString(2),
                    Category = reader.GetString(3),                    
                    ReminderDate = reader.IsDBNull(4) ? (DateTime?)null : reader.GetDateTime(4),
                    IsReminderSet = reader.GetBoolean(5),
                    IsReminderDone = reader.GetBoolean(6),
                    CreatedAt = reader.GetDateTime(7)
                };
                notes.Add(note);
            }
            return notes;
        }

        public async Task MarkReminderAsDoneAsync(int noteId)
        {
            using var connection = _dbService.GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "UPDATE Reminders SET IsReminderDone = 1 WHERE Id = @Id";
            command.Parameters.AddWithValue("@Id", noteId);

            await command.ExecuteNonQueryAsync();
        }

        public async Task DeleteNote(int id)
        {
            await Task.Run(() =>
            {
                using var connection = _dbService.GetConnection();
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText = "DELETE FROM Reminders WHERE Id = @Id";
                command.Parameters.AddWithValue("@Id", id);
                command.ExecuteNonQuery();
            });
        }

        // پاک کردن همه داده‌های جدول
        public void ClearAllData()
        {
            try
            {
                using var connection = _dbService.GetConnection();
                connection.Open();

                var cmd = connection.CreateCommand();
                cmd.CommandText = "DELETE FROM Reminders;"; // همه ردیف‌ها حذف میشه
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ClearAllData error: {ex.Message}");
                throw;
            }
        }

        public List<ReminderModel> SearchNotes(string query)
        {
            var notes = new List<ReminderModel>();
            using var connection = _dbService.GetConnection();
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT * FROM Reminders
                WHERE Title LIKE @query OR Category LIKE @query
                ORDER BY CreatedAt DESC
            ";
            command.Parameters.AddWithValue("@query", $"%{query}%");
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                notes.Add(ReadNote(reader));
            }
            return notes;
        }

        private static ReminderModel ReadNote(SqliteDataReader reader)
        {
            // استفاده از GetOrdinal برای روشن‌تر و مقاومتر شدن در برابر تغییر ترتیب ستون‌ها
            int id = reader.GetInt32(reader.GetOrdinal("Id"));
            string title = reader.GetString(reader.GetOrdinal("Title"));
            string description = reader.GetString(reader.GetOrdinal("Description"));
            string category = reader.GetString(reader.GetOrdinal("Category"));
           
            DateTime? reminder = null;
            int remOrd = reader.GetOrdinal("ReminderDate");
            if (!reader.IsDBNull(remOrd))
            {
                reminder = DateTime.Parse(reader.GetString(remOrd), null, DateTimeStyles.RoundtripKind);
            }
            bool isreminderset = reader.GetBoolean(reader.GetOrdinal("IsReminderSet"));
            bool isreminderdone = reader.GetBoolean(reader.GetOrdinal("IsReminderDone"));
            var createdAt = DateTime.Parse(reader.GetString(reader.GetOrdinal("CreatedAt")), null, DateTimeStyles.RoundtripKind);

            return new ReminderModel
            {
                Id = id,
                Title = title,
                Description = description,
                Category = category,                
                ReminderDate = reminder,
                IsReminderSet = isreminderset,
                IsReminderDone = isreminderdone,
                CreatedAt = createdAt,
            };
        }
    }
}