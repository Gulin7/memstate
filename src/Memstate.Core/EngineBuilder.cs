namespace Memstate
{
    using System.Threading.Tasks;

    public class EngineBuilder
    {
        private readonly MemstateSettings _settings;

        private readonly StorageProvider _storageProvider;

        public EngineBuilder(MemstateSettings settings, StorageProvider storageProvider = null)
        {
            _settings = settings;
            _storageProvider = storageProvider ?? settings.GetStorageProvider();
            _storageProvider.Initialize();
        }

        public Task<Engine<T>> Build<T>() where T : class, new()
        {
            return Build(new T());
        }

        public async Task<Engine<T>> Build<T>(T initialModel) where T : class
        {
            var reader = _storageProvider.CreateJournalReader();
            var loader = new ModelLoader();
            var model = loader.Load(reader, initialModel);
            var nextRecordNumber = loader.LastRecordNumber + 1;

            await reader.DisposeAsync().ConfigureAwait(false);

            var writer = _storageProvider.CreateJournalWriter(nextRecordNumber);
            var subscriptionSource = _storageProvider.CreateJournalSubscriptionSource();
            return new Engine<T>(_settings, model, subscriptionSource, writer, nextRecordNumber);
        }
    }
}