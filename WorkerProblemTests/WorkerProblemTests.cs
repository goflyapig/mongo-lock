using System.Threading.Tasks;
using MongoDBLock;
using NUnit.Framework;
using WorkerProblem;

namespace WorkerProblemTests
{
  internal class WorkerProblemTests
  {
    [Test]
    public async Task MongoDBLock_solves_problem()
    {
      await new WorkerSolutionTester(new MongoDBLockSolution()).VerifyAsync();
    }

    [Test]
    public async Task MongoDBAtomicReplace_solves_problem()
    {
      await new WorkerSolutionTester(new MongoDBAtomicReplaceSolution()).VerifyAsync();
    }
  }
}