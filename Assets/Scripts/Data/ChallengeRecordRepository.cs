using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using Mono.Data.Sqlite;
using UnityEngine;

public static class ChallengeRecordRepository
{
    private const string DatabaseFileName = "challenge_records.sqlite";
    private static bool _initialized;

    public static string DatabasePath
    {
        get { return Path.Combine(Application.persistentDataPath, DatabaseFileName); }
    }

    public static void Save(ChallengeRecord record)
    {
        if (record == null)
            throw new ArgumentNullException("record");

        EnsureDatabase();

        using (SqliteConnection connection = OpenConnection())
        using (IDbCommand command = connection.CreateCommand())
        {
            command.CommandText = @"
INSERT INTO challenge_records
(start_time, end_time, duration_seconds, highest_floor, highest_progress, is_win)
VALUES
(@start_time, @end_time, @duration_seconds, @highest_floor, @highest_progress, @is_win);";
            AddParameter(command, "@start_time", record.StartTime);
            AddParameter(command, "@end_time", record.EndTime);
            AddParameter(command, "@duration_seconds", record.DurationSeconds);
            AddParameter(command, "@highest_floor", record.HighestFloor);
            AddParameter(command, "@highest_progress", record.HighestProgress);
            AddParameter(command, "@is_win", record.IsWin ? 1 : 0);
            command.ExecuteNonQuery();

            command.CommandText = "SELECT last_insert_rowid();";
            object id = command.ExecuteScalar();
            record.Id = Convert.ToInt64(id);
        }
    }

    public static List<ChallengeRecord> LoadLeaderboard(int limit = 20)
    {
        EnsureDatabase();

        List<ChallengeRecord> records = new List<ChallengeRecord>();
        using (SqliteConnection connection = OpenConnection())
        using (IDbCommand command = connection.CreateCommand())
        {
            command.CommandText = @"
SELECT id, start_time, end_time, duration_seconds, highest_floor, highest_progress, is_win
FROM challenge_records
ORDER BY highest_progress DESC, duration_seconds ASC, end_time DESC
LIMIT @limit;";
            AddParameter(command, "@limit", Mathf.Max(1, limit));

            using (IDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    records.Add(new ChallengeRecord
                    {
                        Id = Convert.ToInt64(reader["id"]),
                        StartTime = Convert.ToString(reader["start_time"]),
                        EndTime = Convert.ToString(reader["end_time"]),
                        DurationSeconds = Convert.ToInt32(reader["duration_seconds"]),
                        HighestFloor = Convert.ToInt32(reader["highest_floor"]),
                        HighestProgress = Convert.ToInt32(reader["highest_progress"]),
                        IsWin = Convert.ToInt32(reader["is_win"]) == 1
                    });
                }
            }
        }

        return records;
    }

    public static void EnsureDatabase()
    {
        if (_initialized && File.Exists(DatabasePath))
            return;

        string directory = Path.GetDirectoryName(DatabasePath);
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        using (SqliteConnection connection = OpenConnection())
        using (IDbCommand command = connection.CreateCommand())
        {
            command.CommandText = @"
CREATE TABLE IF NOT EXISTS challenge_records (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    start_time TEXT NOT NULL,
    end_time TEXT NOT NULL,
    duration_seconds INTEGER NOT NULL,
    highest_floor INTEGER NOT NULL,
    highest_progress INTEGER NOT NULL,
    is_win INTEGER NOT NULL
);
CREATE INDEX IF NOT EXISTS idx_challenge_records_rank
ON challenge_records(highest_progress DESC, duration_seconds ASC, end_time DESC);";
            command.ExecuteNonQuery();
        }

        _initialized = true;
    }

    private static SqliteConnection OpenConnection()
    {
        SqliteConnection connection = new SqliteConnection("URI=file:" + DatabasePath);
        connection.Open();
        return connection;
    }

    private static void AddParameter(IDbCommand command, string name, object value)
    {
        IDbDataParameter parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }
}