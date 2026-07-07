using System.Text.Json;
using FindAirConditioners.Web.Core.Abstractions;
using FindAirConditioners.Web.Core.Models;
using Npgsql;

namespace FindAirConditioners.Web.Core.Persistence;

public sealed class PostgresAirConditionerRepository(string connectionString) : IAirConditionerRepository
{
    static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var statements = new[]
        {
            """
            CREATE TABLE IF NOT EXISTS search_requests (
                search_id uuid PRIMARY KEY,
                max_price numeric NULL,
                notification_email text NULL,
                requested_at_utc timestamptz NOT NULL
            );
            """,
            """
            CREATE TABLE IF NOT EXISTS listings (
                id bigserial PRIMARY KEY,
                search_id uuid NOT NULL,
                source text NOT NULL,
                title text NOT NULL,
                price numeric NOT NULL,
                url text NOT NULL,
                image_url text NULL,
                notes text NULL
            );
            """,
            """
            CREATE INDEX IF NOT EXISTS ix_listings_search_id ON listings(search_id);
            """,
            """
            ALTER TABLE listings DROP COLUMN IF EXISTS energy_efficiency_score;
            """,
            """
            ALTER TABLE listings DROP COLUMN IF EXISTS cooling_capacity_kw;
            """,
            """
            ALTER TABLE search_requests DROP COLUMN IF EXISTS postal_code;
            """,
            """
            CREATE TABLE IF NOT EXISTS search_results (
                search_id uuid PRIMARY KEY,
                listings_json text NOT NULL,
                status text NOT NULL,
                summary text NULL,
                created_at_utc timestamptz NOT NULL
            );
            """
        };

        foreach (var statement in statements)
        {
            await using var command = new NpgsqlCommand(statement, connection);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    public async Task SaveRequestedSearchAsync(Guid searchId, AirConditionerSearchRequest request, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO search_requests (
                search_id, max_price, notification_email, requested_at_utc
            ) VALUES (
                @search_id, @max_price, @notification_email, @requested_at_utc
            )
            ON CONFLICT (search_id) DO UPDATE SET
                max_price = EXCLUDED.max_price,
                notification_email = EXCLUDED.notification_email,
                requested_at_utc = EXCLUDED.requested_at_utc;
            """;

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("search_id", searchId);
        command.Parameters.AddWithValue("max_price", (object?)request.MaxPrice ?? DBNull.Value);
        command.Parameters.AddWithValue("notification_email", (object?)request.NotificationEmail ?? DBNull.Value);
        command.Parameters.AddWithValue("requested_at_utc", DateTimeOffset.UtcNow);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task SaveListingsAsync(Guid searchId, IReadOnlyCollection<AirConditionerListing> newListings, CancellationToken cancellationToken = default)
    {
        const string deleteSql = """DELETE FROM listings WHERE search_id = @search_id;""";
        const string insertSql = """
            INSERT INTO listings (
                search_id, source, title, price, url, image_url, notes
            ) VALUES (
                @search_id, @source, @title, @price, @url, @image_url, @notes
            );
            """;

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await using (var delete = new NpgsqlCommand(deleteSql, connection, transaction))
        {
            delete.Parameters.AddWithValue("search_id", searchId);
            await delete.ExecuteNonQueryAsync(cancellationToken);
        }

        foreach (var listing in newListings)
        {
            await using var insert = new NpgsqlCommand(insertSql, connection, transaction);
            insert.Parameters.AddWithValue("search_id", searchId);
            insert.Parameters.AddWithValue("source", listing.Source);
            insert.Parameters.AddWithValue("title", listing.Title);
            insert.Parameters.AddWithValue("price", listing.Price);
            insert.Parameters.AddWithValue("url", listing.Url);
            insert.Parameters.AddWithValue("image_url", (object?)listing.ImageUrl ?? DBNull.Value);
            insert.Parameters.AddWithValue("notes", (object?)listing.Notes ?? DBNull.Value);
            await insert.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<AirConditionerSearchRequest?> GetRequestAsync(Guid searchId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT max_price, notification_email
            FROM search_requests
            WHERE search_id = @search_id;
            """;

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("search_id", searchId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new AirConditionerSearchRequest(
            reader.IsDBNull(0) ? null : reader.GetDecimal(0),
            reader.IsDBNull(1) ? null : reader.GetString(1));
    }

    public async Task<IReadOnlyCollection<AirConditionerListing>> GetListingsAsync(Guid searchId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT source, title, price, url, image_url, notes
            FROM listings
            WHERE search_id = @search_id
            ORDER BY price;
            """;

        var results = new List<AirConditionerListing>();

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("search_id", searchId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new AirConditionerListing(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetDecimal(2),
                reader.GetString(3),
                reader.IsDBNull(4) ? null : reader.GetString(4),
                reader.IsDBNull(5) ? null : reader.GetString(5)));
        }

        return results;
    }

    public async Task SaveResultAsync(AirConditionerSearchResult result, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO search_results (search_id, listings_json, status, summary, created_at_utc)
            VALUES (@search_id, @listings_json, @status, @summary, @created_at_utc)
            ON CONFLICT (search_id) DO UPDATE SET
                listings_json = EXCLUDED.listings_json,
                status = EXCLUDED.status,
                summary = EXCLUDED.summary,
                created_at_utc = EXCLUDED.created_at_utc;
            """;

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("search_id", result.SearchId);
        command.Parameters.AddWithValue("listings_json", JsonSerializer.Serialize(result.Listings, JsonOptions));
        command.Parameters.AddWithValue("status", result.Status);
        command.Parameters.AddWithValue("summary", (object?)result.Summary ?? DBNull.Value);
        command.Parameters.AddWithValue("created_at_utc", result.RequestedAtUtc);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<AirConditionerSearchResult?> GetResultAsync(Guid searchId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT listings_json, status, summary, created_at_utc
            FROM search_results
            WHERE search_id = @search_id;
            """;

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("search_id", searchId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var listings = JsonSerializer.Deserialize<AirConditionerListing[]>(reader.GetString(0), JsonOptions) ?? [];
        return new AirConditionerSearchResult(
            searchId,
            reader.GetFieldValue<DateTimeOffset>(3),
            listings,
            reader.GetString(1),
            reader.IsDBNull(2) ? null : reader.GetString(2));
    }
}
