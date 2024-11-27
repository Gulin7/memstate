using System.Collections.Generic;

namespace Memstate
{
    public interface IJournalReader : IAsyncDisposable
    {
        IAsyncEnumerable<JournalRecord> GetRecords(long fromRecord = 0);
    }
}