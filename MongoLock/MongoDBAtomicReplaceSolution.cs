using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Driver;
using WorkerProblem;

namespace MongoDBLock
{
  public class MongoDBAtomicReplaceSolution : IWorkerSolution
  {
    private const string ConnectionString = "mongodb://localhost:27017";
    private const string DatabaseName = "workerProblemAtomicReplace";

    private readonly IMongoCollection<QueueModel> _queueCollection;

    public MongoDBAtomicReplaceSolution()
    {
      var client = new MongoClient(ConnectionString);
      var database = client.GetDatabase(DatabaseName);
      _queueCollection = database.GetCollection<QueueModel>("queues");
    }

    public void SaveInitialQueue(Queue queue)
    {
      var queueModel = new QueueModel
      {
        Id = queue.Id.ToString(),
        Revision = 1,
        Jobs = new List<JobModel>(
          from job in queue.Jobs
          select new JobModel
          {
            JobNumber = job.JobNumber,
            AssignedWorkerNumber = job.AssignedWorkerNumber
          })
      };

      _queueCollection.InsertOne(queueModel);
    }

    public void ReserveJob(Guid queueId, int forWorkerNumber)
    {
      ReplaceOneResult result;
      do
      {
        var queueModel = _queueCollection.Find(queue => queue.Id == queueId.ToString()).Single();

        int originalRevision = queueModel.Revision;
        queueModel.Revision++;

        var jobToTake = queueModel.Jobs.FirstOrDefault(job => job.AssignedWorkerNumber == null);
        if (jobToTake == null)
          throw new Exception($"No job left to take in queue {queueId}");

        jobToTake.AssignedWorkerNumber = forWorkerNumber;

        result = _queueCollection.ReplaceOne(
          queue => queue.Id == queueId.ToString() && queue.Revision == originalRevision,
          queueModel);
      } while (!result.IsAcknowledged || result.ModifiedCount == 0);
    }

    public Queue GetQueue(Guid queueId)
    {
      var queueModel = _queueCollection.Find(queue => queue.Id == queueId.ToString()).Single();
      return new Queue(
        Guid.Parse(queueModel.Id),
        from jobModel in queueModel.Jobs
        select new Job(jobModel.JobNumber, jobModel.AssignedWorkerNumber));
    }

    private class QueueModel
    {
      public string Id { get; set; }
      public int Revision { get; set; }
      public List<JobModel> Jobs { get; set; }
    }

    private class JobModel
    {
      public int JobNumber { get; set; }
      public int? AssignedWorkerNumber { get; set; }
    }
  }
}