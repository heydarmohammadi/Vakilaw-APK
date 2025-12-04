using Microsoft.Data.Sqlite;
using Vakilaw.Models;

namespace Vakilaw.Services;

public class TransactionService
{
    private readonly DatabaseService _db;

    public TransactionService(DatabaseService db)
    {
        _db = db;
    }

    // 📌 دریافت همه تراکنش‌ها
    public async Task<List<Transaction>> GetAll()
    {
        var list = new List<Transaction>();

        using var conn = _db.GetConnection();
        await conn.OpenAsync();

        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, Title, Amount, IsIncome, Date, Description FROM Transactions ORDER BY Date DESC";

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new Transaction
            {
                Id = reader.GetInt32(0),
                Title = reader.GetString(1),
                Amount = reader.GetDecimal(2),
                IsIncome = reader.GetInt32(3) == 1,
                Date = DateTime.Parse(reader.GetString(4)),
                Description = reader.IsDBNull(5) ? "" : reader.GetString(5)
            });
        }

        return list;
    }

    // 📌 افزودن تراکنش
    public async Task Add(Transaction transaction)
    {
        using var conn = _db.GetConnection();
        await conn.OpenAsync();

        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO Transactions (Title, Amount, IsIncome, Date, Description)
            VALUES ($title, $amount, $isIncome, $date, $desc)";

        cmd.Parameters.AddWithValue("$title", transaction.Title);
        cmd.Parameters.AddWithValue("$amount", transaction.Amount);
        cmd.Parameters.AddWithValue("$isIncome", transaction.IsIncome ? 1 : 0);
        cmd.Parameters.AddWithValue("$date", transaction.Date);       
        cmd.Parameters.AddWithValue("$desc", transaction.Description ?? "");

        await cmd.ExecuteNonQueryAsync();
    }

    public List<Transaction> SearchTransactions(string keyword)
    {
        var transaction = new List<Transaction>();

        using var connection = _db.GetConnection();
        connection.Open();

        var cmd = connection.CreateCommand();

        if (string.IsNullOrWhiteSpace(keyword))
        {
            cmd.CommandText = "SELECT * FROM Transactions";
        }
        else
        {
            cmd.CommandText = @"
            SELECT * FROM Transactions
            WHERE Title LIKE $kw";
            cmd.Parameters.AddWithValue("$kw", "%" + keyword.Trim() + "%");
        }

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            transaction.Add(new Transaction
            {
                Id = reader.GetInt32(0),
                Title = reader.GetString(1),
                Amount = reader.GetDecimal(2),
                IsIncome = reader.GetInt32(3) == 1,
                Date = DateTime.Parse(reader.GetString(4)),
                Description = reader.IsDBNull(5) ? "" : reader.GetString(5)
            });
        }

        return transaction;
    }

    public List<Transaction> SearchTransactionsByDateRange(string fromShamsi, string toShamsi)
    {
        var transactions = new List<Transaction>();

        using var connection = _db.GetConnection();
        connection.Open();

        var cmd = connection.CreateCommand();

        var (fromDate, _) = DatabaseHelper.ConvertShamsiToGregorian(fromShamsi);
        var (toDate, _) = DatabaseHelper.ConvertShamsiToGregorian(toShamsi);

        if (fromDate == null || toDate == null)
        {
            // اگر تاریخ معتبر نبود، چیزی برنگردون
            return transactions;
        }

        // از اول روز شروع، تا اول روز بعد از تاریخ پایانی
        var start = fromDate.Value.Date;
        var end = toDate.Value.Date.AddDays(1);

        cmd.CommandText = "SELECT * FROM Transactions WHERE Date >= $start AND Date < $end";
        cmd.Parameters.AddWithValue("$start", start.ToString("yyyy-MM-dd HH:mm:ss"));
        cmd.Parameters.AddWithValue("$end", end.ToString("yyyy-MM-dd HH:mm:ss"));

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            transactions.Add(new Transaction
            {
                Id = reader.GetInt32(0),
                Title = reader.GetString(1),
                Amount = reader.GetDecimal(2),
                IsIncome = reader.GetInt32(3) == 1,
                Date = DateTime.Parse(reader.GetString(4)),
                Description = reader.IsDBNull(5) ? "" : reader.GetString(5)
            });
        }

        return transactions;
    }


    // 📌 حذف تراکنش
    public async Task Delete(int id)
    {
        using var conn = _db.GetConnection();
        await conn.OpenAsync();

        var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM Transactions WHERE Id = $id";
        cmd.Parameters.AddWithValue("$id", id);

        await cmd.ExecuteNonQueryAsync();
    }

    // 📌 ویرایش تراکنش
    public async Task Update(Transaction transaction)
    {
        using var conn = _db.GetConnection();
        await conn.OpenAsync();

        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            UPDATE Transactions
            SET Title = $title,
                Amount = $amount,
                IsIncome = $isIncome,
                Date = $date,
                Description = $desc
            WHERE Id = $id";

        cmd.Parameters.AddWithValue("$title", transaction.Title);
        cmd.Parameters.AddWithValue("$amount", transaction.Amount);
        cmd.Parameters.AddWithValue("$isIncome", transaction.IsIncome ? 1 : 0);
        cmd.Parameters.AddWithValue("$date", transaction.Date);
        cmd.Parameters.AddWithValue("$desc", transaction.Description ?? "");
        cmd.Parameters.AddWithValue("$id", transaction.Id);

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<decimal> GetTotalIncome()
    {
        using var conn = _db.GetConnection();
        await conn.OpenAsync();

        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT IFNULL(SUM(Amount), 0) FROM Transactions WHERE IsIncome = 1";

        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToDecimal(result);
    }

    public async Task<decimal> GetTotalExpense()
    {
        using var conn = _db.GetConnection();
        await conn.OpenAsync();

        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT IFNULL(SUM(Amount), 0) FROM Transactions WHERE IsIncome = 0";

        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToDecimal(result);
    }
    public async Task<decimal> GetBalance()
    {
        using var conn = _db.GetConnection();
        await conn.OpenAsync();

        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
        SELECT 
            IFNULL(SUM(CASE WHEN IsIncome = 1 THEN Amount ELSE 0 END), 0) -
            IFNULL(SUM(CASE WHEN IsIncome = 0 THEN Amount ELSE 0 END), 0)
        FROM Transactions";

        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToDecimal(result);
    }
}