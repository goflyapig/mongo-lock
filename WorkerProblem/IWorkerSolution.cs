using System;

namespace WorkerProblem
{
  public interface IWorkerSolution
  {
    void SaveInitialQueue(Queue queue);

    void ReserveJob(Guid queueId, int forWorkerNumber);

    Queue GetQueue(Guid queueId);
  }
}