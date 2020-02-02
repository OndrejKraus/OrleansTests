using System;
using Orleans.Hosting;
using Orleans.TestingHost;
using Xunit;

namespace StreamInterleavingTest
{
    public class ClusterFixture : IDisposable
    {
        public ClusterFixture()
        {
            var builder = new TestClusterBuilder();
            builder.AddSiloBuilderConfigurator<TestSiloConfigurations>();
            this.Cluster = builder.Build();
            this.Cluster.Deploy();
        }

        public void Dispose()
        {
            this.Cluster.StopAllSilos();
        }

        public TestCluster Cluster { get; private set; }
    }

    public class TestSiloConfigurations : ISiloBuilderConfigurator
    {
        public void Configure(ISiloHostBuilder hostBuilder)
        {
            hostBuilder
                .AddMemoryGrainStorage(Constants.PubSubStore)
                .AddSimpleMessageStreamProvider(Constants.CmdStreamProvider, options => options.FireAndForgetDelivery = true)
                .AddSimpleMessageStreamProvider(Constants.AckStreamProvider, options => options.FireAndForgetDelivery = true)
                .ConfigureServices(services => {
                    //services.AddSingleton<T, Impl>(...);
                });
        }
    }

    [CollectionDefinition(ClusterCollection.Name)]
    public class ClusterCollection : ICollectionFixture<ClusterFixture>
    {
        public const string Name = "ClusterCollection";
    }
}
