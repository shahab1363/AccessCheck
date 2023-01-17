namespace CheckerApp.Runner.DummyRunner
{
    internal class DummyRunner : IRunner<DummyRunnerConfig>
    {
        private DummyRunnerConfig configuration;
        private string clientId;
        private CancellationTokenSource runCTS;

        private bool _isRunning = false;
        public bool IsRunning
        {
            get => _isRunning;
            private set
            {
                if (_isRunning == value)
                {
                    return;
                }
                _isRunning = value;
                IsRunningChange?.Invoke(this, value);
            }
        }

        private bool _isBusy = false;
        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (_isBusy == value)
                {
                    return;
                }
                _isBusy = value;
                IsBusyChange?.Invoke(this, value);
            }
        }

        public bool IsInitialized { get; private set; }

        public event EventHandler<bool> IsRunningChange;
        public event EventHandler<bool> IsBusyChange;

        public Task Initialize(object configuration, string clientId)
        {
            if (configuration is DummyRunnerConfig config)
            {
                return Initialize(config, clientId);
            }

            throw new NotSupportedException("Invalid configuration object type passed.");
        }

        public Task Initialize(DummyRunnerConfig configuration, string clientId)
        {
            this.configuration = configuration;
            this.clientId = clientId;
            IsInitialized = true;

            return Task.CompletedTask;
        }

        public async Task Start(CancellationToken cancellationToken)
        {
            if (!IsInitialized)
            {
                throw new Exception("Runner is not initialized");
            }

            if (IsRunning)
            {
                throw new Exception("Runner is already running");
            }

            try
            {
                this.IsRunning = true;

                runCTS = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                var ct = runCTS.Token;

                while (!cancellationToken.IsCancellationRequested)
                {
                    await this.RunTask(ct).ConfigureAwait(false);
                    await Task.Delay(configuration.SleepBetweenTasks, ct);
                }
            }
            finally
            {
                this.IsRunning = false;
            }
        }

        public Task Stop()
        {
            if (IsRunning)
            {
                runCTS?.Cancel();
            }

            return Task.CompletedTask;
        }

        public Task Cleanup(CancellationToken cancellationToken)
        {
            return Task.Delay(configuration.CleanUpTaskDelay, cancellationToken);
        }

        private Task RunTask(CancellationToken cancellationToken)
        {
            try
            {
                this.IsBusy = true;
                return Task.Delay(configuration.MainTaskDelay, cancellationToken);
            }
            finally
            {
                this.IsBusy = false;
            }
        }
    }
}
