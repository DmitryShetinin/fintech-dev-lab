using Core.Models;

namespace Application.Abstractions.Submission;

public interface ISubmissionProcessor
{


  Task SubmitOperationAsync(
      Operation operation,
      CancellationToken token);


}
