using FishNet.Object.Prediction;

namespace Runtime
{
    public struct EmptyReplicationData : IReplicateData
    {
        private uint tick;
        public void Dispose() { }
        public uint GetTick() => tick;
        public void SetTick(uint value) => tick = value;
    }
}