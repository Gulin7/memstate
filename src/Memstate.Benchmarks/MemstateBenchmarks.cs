﻿using System;
using System.Linq;
using System.Threading.Tasks;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;

using Memstate.EventStore;
using Memstate.Models;
using Memstate.Models.KeyValue;
using Memstate.Postgresql;

namespace Memstate.Benchmarks
{
    [Config(typeof(MemstateConfig))]
    public class MemstateBenchmarks
    {
        public const int Iterations = 10;

        private Engine<KeyValueStore<int>> _engine;

        [Params(
            //typeof(InMemoryStorageProvider),
            typeof(PostgresProvider),
            typeof(EventStoreProvider))]
        public Type StorageProviderTypes { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            var settings = new MemstateSettings().WithRandomSuffixAppendedToStreamName();

            /* 
            var logProvider = new ConsoleLoggerProvider(filter: (cat, level) => true, includeScopes: false);
            settings.LoggerFactory.AddProvider(logProvider);
            */
            settings.StorageProviderName = StorageProviderTypes.AssemblyQualifiedName;
            settings.SerializerName = "newtonsoft.json";
            var engineBuilder = new EngineBuilder(settings);
            _engine = engineBuilder.Build(new KeyValueStore<int>()).Result;
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            //_engine.DisposeAsync().Wait();
        }
        
        [Benchmark(OperationsPerInvoke = Iterations)]
        public async Task CommandRoundtrip()
        {
            var tasks = Enumerable
                .Range(0, Iterations)
                .Select(i => _engine.Execute(new Set<int>(i.ToString(), i)));

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        private class MemstateConfig : ManualConfig
        {
            public MemstateConfig()
            {
                Add(
                    StatisticColumn.Kurtosis,
                    StatisticColumn.OperationsPerSecond,
                    StatisticColumn.P90,
                    StatisticColumn.P95);
            }
        }
    }
}