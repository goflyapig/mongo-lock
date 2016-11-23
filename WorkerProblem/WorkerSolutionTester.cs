using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace WorkerProblem
{
  public class WorkerSolutionTester
  {
    private readonly IWorkerSolution _solution;

    public WorkerSolutionTester(IWorkerSolution solution)
    {
      _solution = solution;
    }

    public async Task VerifyAsync()
    {
      const int numWorkers = 31;
      const int numJobsPerWorker = 21;
      const int numQueues = 2;

      var queueTasks =
        from queueNumber in Enumerable.Range(1, numQueues)
        select RunAsync(numWorkers, numJobsPerWorker);

      var queues = await Task.WhenAll(queueTasks);

      foreach (var queue in queues)
        VerifyQueue(queue, numWorkers, numJobsPerWorker);
    }

    private async Task<Queue> RunAsync(int numWorkers, int numJobsPerWorker)
    {
      var queueId = InitializeQueue(numWorkers*numJobsPerWorker);

      var workers =
        from workerNumber in Enumerable.Range(1, numWorkers)
        select RunWorkerNode(queueId, workerNumber, numJobsPerWorker);

      await Task.WhenAll(workers);
      Console.WriteLine($"Queue {queueId} has finished");

      return _solution.GetQueue(queueId);
    }

    private Guid InitializeQueue(int numJobsToCreate)
    {
      var jobs = Enumerable.Range(1, numJobsToCreate).Select(num => new Job(num, null));
      var queue = new Queue(Guid.NewGuid(), jobs);

      _solution.SaveInitialQueue(queue);

      return queue.Id;
    }

    private Task RunWorkerNode(Guid queueId, int workerNumber, int numJobsToTake)
    {
      return Task.Run(() =>
      {
        var random = new Random();

        for (var i = 0; i < numJobsToTake; i++)
        {
          _solution.ReserveJob(queueId, workerNumber);
          Thread.Sleep(TimeSpan.FromMilliseconds(random.Next(1, 30)));
        }

        Console.WriteLine($"Queue {queueId} Worker #{workerNumber} is finished");
      });
    }

    private void VerifyQueue(Queue queue, int numWorkers, int numJobsPerWorker)
    {
      Console.WriteLine($"Verifying final state of queue {queue.Id}");

      var workerGroups = (
        from job in queue.Jobs
        group job by job.AssignedWorkerNumber
        into workerGroup
        select new {WorkerNumber = workerGroup.Key, NumAssignments = workerGroup.Count()})
        .ToList();

      var unassignedJob = queue.Jobs.FirstOrDefault(x => x.AssignedWorkerNumber == null);
      if (unassignedJob != null)
        throw new Exception($"Queue {queue.Id}: JobNumber {unassignedJob.JobNumber} was unassigned");

      if (workerGroups.Count != numWorkers)
        throw new Exception($"Queue {queue.Id}: Expected {numWorkers} worker groups, got {workerGroups.Count}");

      var badWorkerGroup = workerGroups.FirstOrDefault(x => x.NumAssignments != numJobsPerWorker);
      if (badWorkerGroup != null)
      {
        throw new Exception(
          $"Queue {queue.Id}: Expected all workers to have {numJobsPerWorker} assignments." +
          $" WorkerNumber {badWorkerGroup.WorkerNumber} had {badWorkerGroup.NumAssignments}");
      }
    }
  }
}