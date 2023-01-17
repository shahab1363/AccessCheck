namespace CheckerApp.Runner
{
    internal interface IRunner
    {
        public Task Start(CancellationToken cancellationToken);
        public Task Stop();
        public Task Cleanup(CancellationToken cancellationToken);

        public bool IsInitialized { get; }
        public bool IsRunning { get; }
        public bool IsBusy { get; }

        public event EventHandler<bool> IsRunningChange;
        public event EventHandler<bool> IsBusyChange;
    }

    internal interface IRunner<T> : IRunner
    {
        public Task Initialize(T configuration, string clientId);
    }
}
