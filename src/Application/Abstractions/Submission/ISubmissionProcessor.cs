using Core.Models;

namespace Application.Abstractions.Submission;

public interface ISubmissionProcessor
{


  Task SubmitOperationAsync(
      string operation,
      CancellationToken token);


}
