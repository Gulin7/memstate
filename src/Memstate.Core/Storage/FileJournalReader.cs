using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Memstate.Configuration;

namespace Memstate
{
    public class FileJournalReader : IJournalReader
    {
        private readonly Stream _journalStream;

        private readonly ISerializer _serializer;

        public FileJournalReader(string fileName)
        {
            var cfg = Config.Current;
            var settings = cfg.GetSettings<EngineSettings>();
            _journalStream = cfg.FileSystem.OpenRead(fileName);
            _serializer = cfg.CreateSerializer();
        }

        public Task DisposeAsync()
        {
            return Task.Run((Action) _journalStream.Dispose);
        }

        public async IAsyncEnumerable<JournalRecord> GetRecords(long fromRecord = 0)
        {
            var records = await Task.Run(() => _serializer.ReadObjects<JournalRecord>(_journalStream));
            await foreach (var record in records)
            {
                if (record.RecordNumber >= fromRecord)
                {
                    yield return record;
                }
            }
        }
    }
}