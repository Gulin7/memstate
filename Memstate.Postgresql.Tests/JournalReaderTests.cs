﻿using System.Threading;

namespace Memstate.Postgresql.Tests
{
    using System;
    using System.Collections.Generic;
    using Domain;
    using Npgsql;
    using Xunit;

    public class JournalReaderTests : IDisposable
    {
        private const string ConnectionString = "Host=localhost; Database=postgres;";

        private readonly PostgresqlProvider _provider;
        private readonly IJournalReader _journalReader;
        private readonly IJournalWriter _journalWriter;
        private readonly ISerializer _serializer;

        public JournalReaderTests()
        {
            var settings = new PostgresqlSettings(new Settings())
            {
                ConnectionString = ConnectionString,
                Table = $"memstate_{Guid.NewGuid():N}",
                SubscriptionStream = $"memstate_{Guid.NewGuid():N}_notifications"
            };
            
            _provider = new PostgresqlProvider(settings);
            
            _provider.Initialize();

            _journalReader = _provider.CreateJournalReader();
            _journalWriter = _provider.CreateJournalWriter(0);

            _serializer = settings.CreateSerializer();
        }

        [Fact]
        public void CanRead()
        {
            var create = new Create(Guid.NewGuid(), "Create a Postgresql driver for Memstate");
            
            InsertCommand(_serializer.Serialize(create));
            
            var journalRecords = _journalReader.GetRecords();
            
            Assert.Single(journalRecords);
        }

        [Fact]
        public void CanWrite()
        {
            var create = new Create(Guid.NewGuid(), "Create a Postgresql driver for Memstate");
            
            _journalWriter.Send(create);
            
            Thread.Sleep(500);
            
            var journalRecords = GetJournalRecords();
            
            Assert.Single(journalRecords);
        }

        public void Dispose()
        {
            _journalReader.Dispose();
            _journalWriter.Dispose();
        }

        private void InsertCommand(byte[] data)
        {
            using (var connection = new NpgsqlConnection(ConnectionString))
            using (var command = connection.CreateCommand())
            {
                connection.Open();
                
                command.CommandText = string.Format(@"
INSERT INTO {0}
(
    command
)
VALUES
(
    @command
);",
                    _provider.Settings.Table);

                command.Parameters.AddWithValue("@command", data);
                
                Assert.Equal(1, command.ExecuteNonQuery());
            }
        }

        private IEnumerable<JournalRecord> GetJournalRecords()
        {
            var journalRecords = new List<JournalRecord>();
            
            using (var connection = new NpgsqlConnection(ConnectionString))
            using (var command = connection.CreateCommand())
            {
                connection.Open();
                
                command.CommandText = string.Format(@"
SELECT
    id,
    written
FROM
    {0}
ORDER BY
    id ASC;",
                    _provider.Settings.Table);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var journalRecord = new JournalRecord((long)reader[0], (DateTime)reader[1], null);
                        
                        journalRecords.Add(journalRecord);
                    }
                }
            }

            return journalRecords;
        }
    }
}