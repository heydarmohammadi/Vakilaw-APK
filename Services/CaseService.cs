using Microsoft.Data.Sqlite;
using Vakilaw.Models;
using Vakilaw.Services;

public class CaseService
{
    private readonly DatabaseService _dbService;

    public CaseService(DatabaseService dbService)
    {
        _dbService = dbService;
    }

    public async Task AddCase(Case caseItem)
    {
        // اگر Client هنوز ذخیره نشده
        if (caseItem.ClientId == 0 && caseItem.Client != null)
        {
            var clientService = new ClientService(_dbService);
            await clientService.AddClient(caseItem.Client);
            caseItem.ClientId = caseItem.Client.Id;
        }

        using var connection = _dbService.GetConnection();
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO Cases 
                (Title, CaseNumber, CourtName, JudgeName, StartDate, EndDate, Status, Description, ClientId)
            VALUES 
                ($title, $caseNumber, $courtName, $judgeName, $startDate, $endDate, $status, $description, $clientId);
            SELECT last_insert_rowid();";

        cmd.Parameters.AddWithValue("$title", caseItem.Title);
        cmd.Parameters.AddWithValue("$caseNumber", caseItem.CaseNumber ?? "");
        cmd.Parameters.AddWithValue("$courtName", caseItem.CourtName ?? "");
        cmd.Parameters.AddWithValue("$judgeName", caseItem.JudgeName ?? "");
        cmd.Parameters.AddWithValue("$startDate", caseItem.StartDate ?? "");
        cmd.Parameters.AddWithValue("$endDate", caseItem.EndDate ?? "");
        cmd.Parameters.AddWithValue("$status", caseItem.Status ?? "");
        cmd.Parameters.AddWithValue("$description", caseItem.Description ?? "");
        cmd.Parameters.AddWithValue("$clientId", caseItem.ClientId);

        // گرفتن Id معتبر
        caseItem.Id = (int)(long)await cmd.ExecuteScalarAsync();

        // ثبت فایل‌های پیوست
        if (caseItem.CaseAttachments != null)
        {
            foreach (var att in caseItem.CaseAttachments)
            {
                att.CaseId = caseItem.Id;
                await AddAttachment(att);
            }
        }
    }

    public List<Case> GetAllCases()
    {
        var cases = new List<Case>();

        using var connection = _dbService.GetConnection();
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM Cases ORDER BY Id DESC";

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var caseItem = new Case
            {
                Id = reader.GetInt32(0),
                Title = reader.GetString(1),
                CaseNumber = reader.GetString(2),
                CourtName = reader.GetString(3),
                JudgeName = reader.GetString(4),
                StartDate = reader.GetString(5),
                EndDate = string.IsNullOrEmpty(reader.GetString(6)) ? null : reader.GetString(6),
                Status = reader.GetString(7),
                Description = reader.GetString(8),
                ClientId = reader.GetInt32(9),
            };

            // بارگذاری موکل
            var clientService = new ClientService(_dbService);
            caseItem.Client = clientService.GetClientById(caseItem.ClientId);

            // بارگذاری پیوست‌ها
            caseItem.CaseAttachments = GetAttachmentsByCase(caseItem.Id);

            cases.Add(caseItem);
        }

        return cases;
    }



    public List<Case> GetCasesByClient(int clientId)
    {
        var cases = new List<Case>();

        using var connection = _dbService.GetConnection();
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM Cases WHERE ClientId = $clientId";
        cmd.Parameters.AddWithValue("$clientId", clientId);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var caseItem = new Case
            {
                Id = reader.GetInt32(0),
                Title = reader.GetString(1),
                CaseNumber = reader.GetString(2),
                CourtName = reader.GetString(3),
                JudgeName = reader.GetString(4),
                StartDate = reader.GetString(5),
                EndDate = string.IsNullOrEmpty(reader.GetString(6)) ? null : reader.GetString(6),
                Status = reader.GetString(7),
                Description = reader.GetString(8),
                ClientId = reader.GetInt32(9),
            };

            // ✅ اضافه کردن فایل‌های پیوست
            caseItem.CaseAttachments = GetAttachmentsByCase(caseItem.Id);

            cases.Add(caseItem);
        }

        return cases;
    }

    public Case GetCaseById(int caseId)
    {
        using var connection = _dbService.GetConnection();
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM Cases WHERE Id = $id";
        cmd.Parameters.AddWithValue("$id", caseId);

        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            var caseItem = new Case
            {
                Id = reader.GetInt32(0),
                Title = reader.GetString(1),
                CaseNumber = reader.GetString(2),
                CourtName = reader.GetString(3),
                JudgeName = reader.GetString(4),
                StartDate = reader.GetString(5),
                EndDate = string.IsNullOrEmpty(reader.GetString(6)) ? null : reader.GetString(6),
                Status = reader.GetString(7),
                Description = reader.GetString(8),
                ClientId = reader.GetInt32(9),
            };

            // ✅ اضافه کردن فایل‌های پیوست
            caseItem.CaseAttachments = GetAttachmentsByCase(caseItem.Id);
            caseItem.Client = new ClientService(_dbService).GetClientById(caseItem.ClientId);

            return caseItem;
        }

        return null;
    }

    public async Task UpdateCase(Case caseItem)
    {
        using var connection = _dbService.GetConnection();
        connection.Open();

        // 1️⃣ بروزرسانی اطلاعات پرونده
        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
        UPDATE Cases
        SET 
            CourtName=$courtName,
            JudgeName=$judgeName,           
            EndDate=$endDate,
            Status=$status,
            Description=$description,
            ClientId=$clientId
        WHERE Id=$id;";
        cmd.Parameters.AddWithValue("$courtName", caseItem.CourtName ?? "");
        cmd.Parameters.AddWithValue("$judgeName", caseItem.JudgeName ?? "");
        cmd.Parameters.AddWithValue("$endDate", caseItem.EndDate ?? "");
        cmd.Parameters.AddWithValue("$status", caseItem.Status ?? "");
        cmd.Parameters.AddWithValue("$description", caseItem.Description ?? "");
        cmd.Parameters.AddWithValue("$clientId", caseItem.ClientId);
        cmd.Parameters.AddWithValue("$id", caseItem.Id);
        await cmd.ExecuteNonQueryAsync();

        // 2️⃣ مدیریت Attachments
        if (caseItem.CaseAttachments != null)
        {
            // دریافت فایل‌های موجود در DB
            var existingAttachments = new List<CaseAttachment>();
            var selectCmd = connection.CreateCommand();
            selectCmd.CommandText = "SELECT Id, FileName, FilePath, FileType FROM CaseAttachments WHERE CaseId=$caseId";
            selectCmd.Parameters.AddWithValue("$caseId", caseItem.Id);

            using var reader = await selectCmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                existingAttachments.Add(new CaseAttachment
                {
                    Id = reader.GetInt32(0),
                    CaseId = caseItem.Id,
                    FileName = reader.IsDBNull(1) ? null : reader.GetString(1),
                    FilePath = reader.IsDBNull(2) ? null : reader.GetString(2),
                    FileType = reader.IsDBNull(3) ? null : reader.GetString(3)
                });
            }

            // حذف فایل‌هایی که دیگر در لیست نیستند
            foreach (var oldAtt in existingAttachments)
            {
                if (!caseItem.CaseAttachments.Any(a => a.Id == oldAtt.Id))
                {
                    var delCmd = connection.CreateCommand();
                    delCmd.CommandText = "DELETE FROM CaseAttachments WHERE Id=$id";
                    delCmd.Parameters.AddWithValue("$id", oldAtt.Id);
                    await delCmd.ExecuteNonQueryAsync();
                }
            }

            // اضافه کردن فقط فایل‌های کاملاً جدید (Id=0 یا FilePath جدید)
            var newAttachments = caseItem.CaseAttachments
                .Where(a => a.Id == 0 || !existingAttachments.Any(e => e.FilePath == a.FilePath))
                .ToList();

            foreach (var newAtt in newAttachments)
            {
                newAtt.CaseId = caseItem.Id;
                await AddAttachment(newAtt);
            }
        }
    }

    public async Task DeleteCase(int caseId)
    {
        using var connection = _dbService.GetConnection();
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "DELETE FROM Cases WHERE Id=$id";
        cmd.Parameters.AddWithValue("$id", caseId);

        await cmd.ExecuteNonQueryAsync();
    }

    public List<Case> SearchCases(string keyword)
    {
        var cases = new List<Case>();

        using var connection = _dbService.GetConnection();
        connection.Open();

        var cmd = connection.CreateCommand();

        if (string.IsNullOrWhiteSpace(keyword))
        {
            cmd.CommandText = @"
        SELECT c.Id, c.Title, c.CaseNumber, c.CourtName, c.JudgeName,
               c.StartDate, c.EndDate, c.Status, c.Description,
               c.ClientId, cl.FullName
        FROM Cases c
        JOIN Clients cl ON c.ClientId = cl.Id";
        }
        else
        {
            cmd.CommandText = @"
        SELECT c.Id, c.Title, c.CaseNumber, c.CourtName, c.JudgeName,
               c.StartDate, c.EndDate, c.Status, c.Description,
               c.ClientId, cl.FullName
        FROM Cases c
        JOIN Clients cl ON c.ClientId = cl.Id
        WHERE cl.FullName LIKE $kw";
            cmd.Parameters.AddWithValue("$kw", "%" + keyword.Trim() + "%");
        }

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var caseItem = new Case
            {
                Id = reader.GetInt32(0),
                Title = reader.IsDBNull(1) ? null : reader.GetString(1),
                CaseNumber = reader.IsDBNull(2) ? null : reader.GetString(2),
                CourtName = reader.IsDBNull(3) ? null : reader.GetString(3),
                JudgeName = reader.IsDBNull(4) ? null : reader.GetString(4),
                StartDate = reader.IsDBNull(5) ? null : reader.GetString(5),
                EndDate = reader.IsDBNull(6) ? null : reader.GetString(6),
                Status = reader.IsDBNull(7) ? null : reader.GetString(7),
                Description = reader.IsDBNull(8) ? null : reader.GetString(8),
                ClientId = reader.GetInt32(9),
                Client = new Client
                {
                    Id = reader.GetInt32(9),
                    FullName = reader.GetString(10)
                }
            };

            // ✅ اضافه کردن فایل‌های پیوست
            caseItem.CaseAttachments = GetAttachmentsByCase(caseItem.Id);

            cases.Add(caseItem);
        }

        return cases;
    }

    public async Task<int> GetCasesCount()
    {
        await using var connection = _dbService.GetConnection();
        await connection.OpenAsync();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM Cases";

        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    #region Attachments

    // افزودن فایل پیوست
    public async Task AddAttachment(CaseAttachment attachment)
    {
        using var connection = _dbService.GetConnection();
        connection.Open();

        // اول مطمئن شو CaseId وجود داره
        var checkCaseCmd = connection.CreateCommand();
        checkCaseCmd.CommandText = "SELECT COUNT(*) FROM Cases WHERE Id=$caseId";
        checkCaseCmd.Parameters.AddWithValue("$caseId", attachment.CaseId);

        var caseExists = (long)await checkCaseCmd.ExecuteScalarAsync() > 0;
        if (!caseExists)
            throw new Exception("CaseId نامعتبر است، پرونده در دیتابیس وجود ندارد.");

        // بررسی تکراری نبودن فایل
        var checkCmd = connection.CreateCommand();
        checkCmd.CommandText = @"
            SELECT COUNT(*) 
            FROM CaseAttachments 
            WHERE CaseId = $caseId AND FilePath = $filePath;";
        checkCmd.Parameters.AddWithValue("$caseId", attachment.CaseId);
        checkCmd.Parameters.AddWithValue("$filePath", attachment.FilePath ?? "");

        var exists = (long)await checkCmd.ExecuteScalarAsync() > 0;
        if (exists) return;

        // درج فایل جدید
        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO CaseAttachments (CaseId, FileName, FilePath, FileType)
            VALUES ($caseId, $fileName, $filePath, $fileType);";

        cmd.Parameters.AddWithValue("$caseId", attachment.CaseId);
        cmd.Parameters.AddWithValue("$fileName", attachment.FileName ?? "");
        cmd.Parameters.AddWithValue("$filePath", attachment.FilePath ?? "");
        cmd.Parameters.AddWithValue("$fileType", attachment.FileType ?? "");

        await cmd.ExecuteNonQueryAsync();
    }

    // دریافت همه فایل‌های یک پرونده
    public List<CaseAttachment> GetAttachmentsByCase(int caseId)
    {
        var list = new List<CaseAttachment>();

        using var connection = _dbService.GetConnection();
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT Id, CaseId, FileName, FilePath, FileType FROM CaseAttachments WHERE CaseId=$caseId";
        cmd.Parameters.AddWithValue("$caseId", caseId);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new CaseAttachment
            {
                Id = reader.GetInt32(0),
                CaseId = reader.GetInt32(1),
                FileName = reader.IsDBNull(2) ? null : reader.GetString(2),
                FilePath = reader.IsDBNull(3) ? null : reader.GetString(3),
                FileType = reader.IsDBNull(4) ? null : reader.GetString(4)
            });
        }

        return list;
    }

    public async Task<bool> DeleteAttachment(int attachmentId)
    {
        using var connection = _dbService.GetConnection();
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "DELETE FROM CaseAttachments WHERE Id=$id";
        cmd.Parameters.AddWithValue("$id", attachmentId);

        var affectedRows = await cmd.ExecuteNonQueryAsync();
        return affectedRows > 0;
    }

    #endregion
}