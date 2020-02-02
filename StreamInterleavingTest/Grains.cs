using System;
using System.Threading.Tasks;
using Orleans;
using Orleans.Concurrency;
using Orleans.Streams;

namespace StreamInterleavingTest
{

    public interface IStreamingGrain<T> : IGrainWithGuidKey
    {
        Task Initialize(Guid cmdStreamId, Guid ackStreamId);

        Task<int> Proceed(int members);
    }

    public class Master : Grain, IStreamingGrain<Master>
    {
        private IStreamProvider cmdStreamProvider;
        private IStreamProvider ackStreamProvider;

        private IAsyncStream<Cmd> cmdStream;
        private IAsyncStream<Ack> ackStream;

        private StreamSubscriptionHandle<Cmd> cmdHandle;
        private StreamSubscriptionHandle<Ack> ackHandle;

        private int seenMembers;
        private int totalMembers;

        private int interleaveFactor;
        private int maxInterleaveFactor;

        private readonly TaskCompletionSource<int> done = new TaskCompletionSource<int>();

        public override Task OnActivateAsync()
        {
            Logger.Output.WriteLine("Master: OnActivateAsync");

            cmdStreamProvider = GetStreamProvider(Constants.CmdStreamProvider);
            ackStreamProvider = GetStreamProvider(Constants.AckStreamProvider);

            return base.OnActivateAsync();
        }

        public async Task Initialize(Guid cmdStreamId, Guid ackStreamId)
        {
            Logger.Output.WriteLine("Master: Initialize");

            cmdStream = cmdStreamProvider.GetStream<Cmd>(cmdStreamId, Constants.CmdStreamNamespace);
            ackStream = ackStreamProvider.GetStream<Ack>(ackStreamId, Constants.AckStreamNamespace);

            cmdHandle = await cmdStream.SubscribeAsync(OnCmd);
            ackHandle = await ackStream.SubscribeAsync(OnAck);
        }

        public async Task<int> Proceed(int members)
        {
            Logger.Output.WriteLine("Master: Proceed");

            totalMembers = members;
            await cmdStream.OnNextAsync(new Cmd());

            return await done.Task;
        }

        public async Task OnCmd(Cmd cmd, StreamSequenceToken token)
        {
            Logger.Output.WriteLine("Master: > OnCmd");
            await ProcessMember();
            Logger.Output.WriteLine("Master: < OnCmd");
        }

        private async Task ProcessMember()
        {
            interleaveFactor++;
            maxInterleaveFactor = Math.Max(interleaveFactor, maxInterleaveFactor);
            var myFactor = maxInterleaveFactor;

            Logger.Output.WriteLine($"Master: ProcessMember:{myFactor} delaying");
            await Task.Delay(TimeSpan.FromSeconds(1));
            interleaveFactor--;

            seenMembers++;
            Logger.Output.WriteLine($"Master: ProcessMember:{myFactor} seen:{seenMembers}");
            if (seenMembers == totalMembers)
            {
                done.SetResult(maxInterleaveFactor);
            }
        }

        public async Task OnAck(Ack ack, StreamSequenceToken token)
        {
            Logger.Output.WriteLine("Master: > OnAck");
            await ProcessMember();
            Logger.Output.WriteLine("Master: < OnAck");
        }
    }

    public class Slave : Grain, IStreamingGrain<Slave>
    {
        private IStreamProvider cmdStreamProvider;
        private IStreamProvider ackStreamProvider;

        private IAsyncStream<Cmd> cmdStream;
        private IAsyncStream<Ack> ackStream;

        private StreamSubscriptionHandle<Cmd> cmdHandle;

        private Guid myId;

        public override Task OnActivateAsync()
        {
            myId = this.GetPrimaryKey();

            Logger.Output.WriteLine($"Slave({myId}): OnActivateAsync");

            cmdStreamProvider = GetStreamProvider(Constants.CmdStreamProvider);
            ackStreamProvider = GetStreamProvider(Constants.AckStreamProvider);

            return base.OnActivateAsync();
        }

        public async Task Initialize(Guid cmdStreamId, Guid ackStreamId)
        {
            Logger.Output.WriteLine($"Slave({myId}): Initialize");

            cmdStream = cmdStreamProvider.GetStream<Cmd>(cmdStreamId, Constants.CmdStreamNamespace);
            ackStream = ackStreamProvider.GetStream<Ack>(ackStreamId, Constants.AckStreamNamespace);

            cmdHandle = await cmdStream.SubscribeAsync(OnCmd);
        }

        public Task<int> Proceed(int members)
        {
            throw new NotSupportedException();
        }

        public Task OnCmd(Cmd cmd, StreamSequenceToken token)
        {
            Logger.Output.WriteLine($"Slave({myId}): OnCmd");
            return ackStream.OnNextAsync(new Ack(cmd.CmdId));
        }
    }

    [Immutable]
    public class Cmd
    {
        public Cmd()
        {
            CmdId = Guid.NewGuid();
        }

        public Guid CmdId { get; }
    }

    [Immutable]
    public class Ack
    {
        public Ack(Guid cmdId)
        {
            CmdId = cmdId;
            AckId = Guid.NewGuid();
        }

        public Guid CmdId { get; }
        public Guid AckId { get; }

    }
}
