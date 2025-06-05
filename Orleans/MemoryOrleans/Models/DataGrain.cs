namespace MemoryOrleans.Models
{
    public interface IDataGrain : IGrainWithStringKey
    {
        Task SetData(byte[] data);
        Task<long> GetDataSize();
    }

    public class DataGrain : Grain, IDataGrain
    {
        private byte[]? _data;

        public Task SetData(byte[] data)
        {
            _data = data;
            return Task.CompletedTask;
        }

        public Task<long> GetDataSize()
        {
            return Task.FromResult(_data?.LongLength ?? 0);
        }
    }
}
