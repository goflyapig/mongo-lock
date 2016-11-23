using System.Threading.Tasks;
using MongoDBLock;
using NUnit.Framework;
using WorkerProblem;
using Task = System.Threading.Tasks.Task;

namespace WorkerProblemTests
{
  internal class WorkerProblemTests
  {
    [Test]
    public async Task MongoDBLock_solves_problem()
    {
      var tester = new WorkerSolutionTester(new MongoDBLockSolution());
      await tester.VerifyAsync();
    }
  }
}