using System;
using System.Collections.Generic;
using System.Linq;

namespace WorkerProblem
{
  public class Queue
  {
    public Queue(Guid id, IEnumerable<Job> jobs)
    {
      Id = id;
      Jobs = jobs.ToList().AsReadOnly();
    }

    public Guid Id { get; }

    public IReadOnlyList<Job> Jobs { get; }
  }
}