namespace Application.Abstractions.Submission;

public interface ISubmissionProcessor
{
  Task SubmitOperationAsync(
      CancellationToken cancellationToken);
}
