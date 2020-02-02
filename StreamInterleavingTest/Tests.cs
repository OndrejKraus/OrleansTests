using System;
using System.Threading.Tasks;
using Orleans.TestingHost;
using Xunit;
using Xunit.Abstractions;

namespace StreamInterleavingTest
{
    [Collection(ClusterCollection.Name)]
    public class StreamingGrainTests
    {
        private readonly TestCluster _cluster;

        public StreamingGrainTests(ClusterFixture fixture, ITestOutputHelper output)
        {
            _cluster = fixture.Cluster;
            Logger.Output = output;
        }

        [Fact]
        public async Task InterleavesCorrectly()
        {
            Guid cmdStreamId = Guid.NewGuid();
            Guid ackStreamId = Guid.NewGuid();

            var master = _cluster.GrainFactory.GetGrain<IStreamingGrain<Master>>(Guid.NewGuid());
            var slave1 = _cluster.GrainFactory.GetGrain<IStreamingGrain<Slave>>(Guid.NewGuid());
            var slave2 = _cluster.GrainFactory.GetGrain<IStreamingGrain<Slave>>(Guid.NewGuid());

            await master.Initialize(cmdStreamId, ackStreamId);
            await slave1.Initialize(cmdStreamId, ackStreamId);
            await slave2.Initialize(cmdStreamId, ackStreamId);

            var interleaveFactor = await master.Proceed(3);

            Assert.Equal(1, interleaveFactor);
        }
    }

    public static class Logger
    {
        public static ITestOutputHelper Output { get; set; }
    }


}
