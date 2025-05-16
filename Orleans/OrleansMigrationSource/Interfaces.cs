
using Orleans;
using Orleans.Runtime;
using Orleans.Timers;

namespace Orleans.Migration.Source
{
    /// <summary>
    /// mock grain interface for migration tests
    /// </summary>
    public interface ISimplePersistentMigrationGrain : ISimplePersistentGrain
    {
        Task RegisterReminder(string reminderName);
    }

    public interface ISimplePersistentGrain : ISimpleGrain
    {
        //Task CreateReminder();
        //Task<IGrainReminder> GetReminder();

        Task SetA(int a, bool deactivate);
        Task<Guid> GetVersion();
        Task<object> GetRequestContext();
        Task SetRequestContext(int data);
    }

    public interface ISimpleGrain : IGrainWithIntegerKey
    {
        Task SetA(int a);
        Task SetB(int b);
        Task IncrementA();
        Task<int> GetAxB();
        Task<int> GetAxB(int a, int b);
        Task<int> GetA();
    }

    [Serializable]
    public class MigrationTestGrain_State
    {
        public int A { get; set; }
        public int B { get; set; }
    }

    /// <summary>
    /// A simple grain that allows to set two arguments and then multiply them.
    /// </summary>
    [GrainType("migrationtestgrain")]
    public class MigrationTestGrain : Grain<MigrationTestGrain_State>, ISimplePersistentMigrationGrain, IRemindable
    {
        private Guid version;
        private IGrainReminder? _reminder;

        private readonly IReminderRegistry _reminderRegistry;
        private readonly ITimerRegistry _timerRegistry;

        public MigrationTestGrain(
            ITimerRegistry timerRegistry,
            IReminderRegistry reminderRegistry)
        {
            _timerRegistry = timerRegistry;
            _reminderRegistry = reminderRegistry;
        }

        public Task SetA(int a)
        {
            State.A = a;
            return WriteStateAsync();
        }

        public Task SetA(int a, bool deactivate)
        {
            if (deactivate)
                DeactivateOnIdle();
            return SetA(a);
        }

        public Task SetB(int b)
        {
            State.B = b;
            return WriteStateAsync();
        }

        public async Task RegisterReminder(string reminderName)
        {
            await _reminderRegistry.RegisterOrUpdateReminder(reminderName, dueTime: TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(80));
        }

        public Task ReceiveReminder(string reminderName, TickStatus status)
        {
            Console.WriteLine("received reminder: " + reminderName + "; with status: " + status);
            return Task.CompletedTask;
        }

        public Task IncrementA()
        {
            State.A++;
            return WriteStateAsync();
        }

        public Task<int> GetAxB()
        {
            return Task.FromResult(State.A * State.B);
        }

        public Task<int> GetAxB(int a, int b)
        {
            return Task.FromResult(a * b);
        }

        public Task<int> GetA()
        {
            return Task.FromResult(State.A);
        }

        public Task<Guid> GetVersion()
        {
            return Task.FromResult(version);
        }

        public Task<object> GetRequestContext()
        {
            var info = RequestContext.Get("GrainInfo");
            return Task.FromResult(info);
        }

        public Task SetRequestContext(int data)
        {
            RequestContext.Set("GrainInfo", data);
            return Task.CompletedTask;
        }
    }
}
