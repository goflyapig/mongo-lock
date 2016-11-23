using System;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using WorkerProblem;

namespace MongoDBLock
{
  public class MongoDBLockSolution : IWorkerSolution
  {
    private const string ConnectionString = "mongodb://localhost:27017/workerProblem";

    static MongoDBLockSolution()
    {
      BsonClassMap.RegisterClassMap<Queue>(cm =>
      {
        cm.MapIdMember(x => x.Id).SetSerializer(new GuidSerializer(BsonType.String));
        cm.MapMember(x => x.Jobs);
        cm.MapCreator(x => new Queue(x.Id, x.Jobs));
      });

      BsonClassMap.RegisterClassMap<Job>(cm =>
      {
        cm.MapMember(x => x.JobNumber);
        cm.MapMember(x => x.AssignedWorkerNumber);
        cm.MapCreator(x => new Job(x.JobNumber, x.AssignedWorkerNumber));
      });
    }

    public void SaveInitialQueue(Queue queue)
    {
      GetQueueCollection().InsertOne(queue);
    }

    public void ReserveJob(Guid queueId, int forWorkerNumber)
    {
      ReserveJobAsync(queueId, forWorkerNumber).Wait();
    }

    public Queue GetQueue(Guid queueId)
    {
      return GetQueueCollection().Find(Builders<Queue>.Filter.Eq(x => x.Id, queueId)).Single();
    }

    private Task ReserveJobAsync(Guid queueId, int forWorkerNumber)
    {
      var lockEngine = new ExclusiveLockEngine(
        new GlobalExclusiveLock(ConnectionString, TimeSpan.FromHours(1)),
        TimeSpan.FromMilliseconds(1));

      var clientId = $"Worker#{forWorkerNumber}";

      var tcs = new TaskCompletionSource<bool>();

      Console.WriteLine( $"Queue {queueId} Worker #{forWorkerNumber} is waiting for lock..." );
      lockEngine.StartCheckingLock(
        clientId,
        () =>
        {
          Console.WriteLine( $"Queue {queueId} Worker #{forWorkerNumber} obtained lock" );
          ReserveJobInsideLock( queueId, forWorkerNumber );
          Console.WriteLine( $"Queue {queueId} Worker #{forWorkerNumber} is releasing lock" );
          lockEngine.StopCheckingOrReleaseLock( clientId );
          tcs.SetResult(true);
        },
        reason => tcs.SetException( new Exception( $"Lost lock! Reason: {reason}" ) ) );

      return tcs.Task;
    }

    private void ReserveJobInsideLock(Guid queueId, int forWorkerNumber)
    {
      var queueCollection = GetQueueCollection();
      var queueFilter = Builders<Queue>.Filter.Eq(x => x.Id, queueId);
      var queue = queueCollection.Find(queueFilter).Single();
      var jobToTake = queue.Jobs.FirstOrDefault(x => x.AssignedWorkerNumber == null);

      if (jobToTake == null)
        throw new Exception($"No job left to take in queue {queueId}");

      var updatedJobs =
        from job in queue.Jobs
        select job == jobToTake
          ? new Job(job.JobNumber, forWorkerNumber)
          : job;

      var updatedQueue = new Queue(queue.Id, updatedJobs);
      queueCollection.ReplaceOne(queueFilter, updatedQueue);
    }

    private IMongoCollection<Queue> GetQueueCollection()
    {
      return Util.GetDatabaseConnectionString(ConnectionString).GetCollection<Queue>("queues");
    }
  }
}