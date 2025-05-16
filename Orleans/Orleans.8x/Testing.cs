namespace Orleans._8x
{
    public interface ITestGrain : IGrainWithIntegerKey
    {
        Task<int> GetValue();
        Task SetValue(int value);
    }

    [Serializable]
    public class State
    {
        public int Value { get; set; }
    }
    // cant have underlying storage
    #region GrainWithoutInheritance

    public class NoInheritanceGrain : ITestGrain
    {
        int value;

        public Task<int> GetValue()
        {
            return Task.FromResult(value);
        }

        public Task SetValue(int value)
        {
            this.value = value;
            return Task.CompletedTask;
        }
    }

    #endregion

    #region NonGenericGrain

    public class StandardGrain : Grain, ITestGrain
    {
        int value;

        public Task<int> GetValue()
        {
            return Task.FromResult(value);
        }

        public Task SetValue(int value)
        {
            this.value = value;
            return Task.CompletedTask;
        }
    }

    #endregion

    public class GrainT : Grain<State>, ITestGrain
    {
        public Task<int> GetValue()
        {
            return Task.FromResult(State.Value);
        }

        public Task SetValue(int value)
        {
            State.Value = value;
            return WriteStateAsync();
        }
    }

    public class PersistentGrain : ITestGrain
    {
        IPersistentState<State> _persistentState;

        public PersistentGrain([PersistentState("")] IPersistentState<State> state)
        {
            _persistentState = state;
        }

        public Task<int> GetValue()
        {
            return Task.FromResult(_persistentState.State.Value);
        }

        public Task SetValue(int value)
        {
            _persistentState.State.Value = value;
            return _persistentState.WriteStateAsync();
        }
    }
}
