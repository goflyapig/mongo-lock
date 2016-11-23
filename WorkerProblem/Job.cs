namespace WorkerProblem
{
  public class Job
  {
    public Job(int jobNumber, int? assignedWorkerNumber)
    {
      JobNumber = jobNumber;
      AssignedWorkerNumber = assignedWorkerNumber;
    }

    public int JobNumber { get; }

    public int? AssignedWorkerNumber { get; }
  }
}