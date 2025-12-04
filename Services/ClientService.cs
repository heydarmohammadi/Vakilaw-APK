using Microsoft.Data.Sqlite;
using Vakilaw.Models;
using Vakilaw.Services;

public class ClientService
{
    private readonly DatabaseService _dbService;

    public ClientService(DatabaseService dbService)
    {
        _dbService = dbService;
    }

    public async Task AddClient(Client client)
    {
        using var connection = _dbService.GetConnection();
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO Clients (FullName, NationalCode, PhoneNumber, Address, Description)
            VALUES ($fullName, $nationalCode, $phoneNumber, $address, $description);
            SELECT last_insert_rowid();";

        cmd.Parameters.AddWithValue("$fullName", client.FullName);
        cmd.Parameters.AddWithValue("$nationalCode", client.NationalCode ?? "");
        cmd.Parameters.AddWithValue("$phoneNumber", client.PhoneNumber ?? "");
        cmd.Parameters.AddWithValue("$address", client.Address ?? "");
        cmd.Parameters.AddWithValue("$description", client.Description ?? "");

        client.Id = (int)(long)await cmd.ExecuteScalarAsync();
    }

    public List<Client> GetClients()
    {
        var clients = new List<Client>();

        using var connection = _dbService.GetConnection();
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM Clients";

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            clients.Add(new Client
            {
                Id = reader.GetInt32(0),
                FullName = reader.GetString(1),
                NationalCode = reader.GetString(2),
                PhoneNumber = reader.GetString(3),
                Address = reader.GetString(4),
                Description = reader.GetString(5)
            });
        }

        return clients;
    }

    public async Task UpdateClient(Client client)
    {
        using var connection = _dbService.GetConnection();
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            UPDATE Clients
            SET PhoneNumber=$phoneNumber,
                Address=$address,
                Description=$description
            WHERE Id=$id;";
      
        cmd.Parameters.AddWithValue("$phoneNumber", client.PhoneNumber ?? "");
        cmd.Parameters.AddWithValue("$address", client.Address ?? "");
        cmd.Parameters.AddWithValue("$description", client.Description ?? "");
        cmd.Parameters.AddWithValue("$id", client.Id);

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteClient(int clientId)
    {
        using var connection = _dbService.GetConnection();
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "DELETE FROM Clients WHERE Id=$id";
        cmd.Parameters.AddWithValue("$id", clientId);

        await cmd.ExecuteNonQueryAsync();
    }

    public List<Client> SearchClients(string keyword)
    {
        var clients = new List<Client>();

        using var connection = _dbService.GetConnection();
        connection.Open();

        var cmd = connection.CreateCommand();

        if (string.IsNullOrWhiteSpace(keyword))
        {
            cmd.CommandText = "SELECT * FROM Clients";
        }
        else
        {
            cmd.CommandText = @"
            SELECT * FROM Clients
            WHERE FullName LIKE $kw OR PhoneNumber LIKE $kw";
            cmd.Parameters.AddWithValue("$kw", "%" + keyword.Trim() + "%");
        }

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            clients.Add(new Client
            {
                Id = reader.GetInt32(0),
                FullName = reader.GetString(1),
                NationalCode = reader.IsDBNull(2) ? null : reader.GetString(2),
                PhoneNumber = reader.IsDBNull(3) ? null : reader.GetString(3),
                Address = reader.IsDBNull(4) ? null : reader.GetString(4),
                Description = reader.IsDBNull(5) ? null : reader.GetString(5)
            });
        }

        return clients;
    }

    public async Task<int> GetClientsCount()
    {
        await using var connection = _dbService.GetConnection();
        await connection.OpenAsync();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM Clients";

        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    public Client GetClientById(int clientId)
    {
        using var connection = _dbService.GetConnection();
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM Clients WHERE Id = $id";
        cmd.Parameters.AddWithValue("$id", clientId);

        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            return new Client
            {
                Id = reader.GetInt32(0),
                FullName = reader.IsDBNull(1) ? null : reader.GetString(1),
                NationalCode = reader.IsDBNull(2) ? null : reader.GetString(2),
                PhoneNumber = reader.IsDBNull(3) ? null : reader.GetString(3),
                Address = reader.IsDBNull(4) ? null : reader.GetString(4),
                Description = reader.IsDBNull(5) ? null : reader.GetString(5)
            };
        }

        return null;
    }

}