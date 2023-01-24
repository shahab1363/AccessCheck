using Checker.Checks;
using Checker.Configuration;
using CheckerLib.Extensions;

namespace CheckerApp.Runner.CheckerRunner
{
    internal class StepCache
    {
        public StepCache? BeforeStepCache { get; set; }
        public IDictionary<CheckGroup, List<ICheck>> PeriodicChecks { get; set; }
        public Task RunningTask { get; set; }
        public StepCache? AfterStepCache { get; set; }

        public Task CleanUpTask => Task.WhenAll(this.RunningTask, this.BeforeStepCache?.CleanUpTask ?? Task.CompletedTask, this.AfterStepCache?.CleanUpTask ?? Task.CompletedTask);

        //public IDictionary<CheckGroup, List<ICheck>> runBeforePeriodicChecks { get; set; }
        //public Task runBeforeStepTask { get; set; }
        //public IDictionary<CheckGroup, List<ICheck>> runAfterPeriodicCheck { get; set; }
        //public Task runAfterStepTask { get; set; }

        public StepCache(CheckerStep step)
        {
            this.PeriodicChecks = step.CheckGroups.LoadCheckGroups();
            this.RunningTask = Task.CompletedTask;

            this.BeforeStepCache = step.RunBeforeStep != null
                ? new StepCache(step.RunBeforeStep)
                : null;

            this.AfterStepCache = step.RunAfterStep != null
                ? new StepCache(step.RunAfterStep) 
                : null;
        }
        //public StepCache(IDictionary<CheckGroup, List<ICheck>> periodicCheks, Task runningChecksTask, IDictionary<CheckGroup, List<ICheck>> runBeforePeriodicChecks, Task runBeforeStepTask, IDictionary<CheckGroup, List<ICheck>> runAfterPeriodicCheck, Task runAfterStepTask)
        //{
        //    this.PeriodicChecks = periodicCheks;
        //    this.RunningTask = runningChecksTask;
        //    this.runBeforePeriodicChecks = runBeforePeriodicChecks;
        //    this.runBeforeStepTask = runBeforeStepTask;
        //    this.runAfterPeriodicCheck = runAfterPeriodicCheck;
        //    this.runAfterStepTask = runAfterStepTask;
        //}
    }
}
