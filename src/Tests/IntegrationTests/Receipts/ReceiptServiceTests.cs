using Application.Interface;
using Application.Receipts;
using Application.Receipts.Requests;
using Core.Enums;
using Core.Models;
using FluentAssertions;
using Moq;

namespace Application.Tests.Receipts;

public sealed class ReceiptServiceTests
{
  private readonly Mock<IOperationRepository> _repository = new();
  private readonly Mock<IUnitOfWork> _unitOfWork = new();

  private readonly OperationStateMachine _stateMachine = new();

  private readonly ReceiptService _service;

  public ReceiptServiceTests()
  {
    _service = new ReceiptService(
        _repository.Object,
        _stateMachine,
        _unitOfWork.Object);
  }

  [Fact]
  public async Task ProcessAsync_ShouldReturnFailure_WhenOperationDoesNotExist()
  {
    // Arrange
    _repository
        .Setup(x => x.GetByIdAsync(
            "operation-1",
            It.IsAny<CancellationToken>()))
        .ReturnsAsync((Operation?)null);

    var request = new ReceiptRequest
    {
      OperationId = "operation-1",
      ProviderPaymentId = "payment-1",
      Result = ReceiptResult.COMPLETED
    };

    // Act
    var result = await _service.ProcessAsync(
        request,
        CancellationToken.None);

    // Assert
    result.IsSuccess.Should().BeFalse();

    _unitOfWork.Verify(
        x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
        Times.Never);
  }

  [Fact]
  public async Task ProcessAsync_ShouldIgnoreDuplicateReceipt()
  {
    // Arrange
    var operation = Operation.Create(
        "operation-1",
        100,
        "RUB",
        "test");

    operation.SetProviderPaymentId("payment-1");

    _repository
        .Setup(x => x.GetByIdAsync(
            operation.Id,
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(operation);

    var request = new ReceiptRequest
    {
      OperationId = operation.Id,
      ProviderPaymentId = "payment-1",
      Result = ReceiptResult.COMPLETED
    };

    // Act
    var result = await _service.ProcessAsync(
        request,
        CancellationToken.None);

    // Assert
    result.IsSuccess.Should().BeTrue();

    _unitOfWork.Verify(
        x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
        Times.Never);
  }

  [Fact]
  public async Task ProcessAsync_ShouldReturnFailure_WhenProviderPaymentIdDoesNotMatch()
  {
    // Arrange
    var operation = Operation.Create(
        "operation-1",
        100,
        "RUB",
        "test");

    operation.SetProviderPaymentId("payment-1");

    _repository
        .Setup(x => x.GetByIdAsync(
            operation.Id,
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(operation);

    var request = new ReceiptRequest
    {
      OperationId = operation.Id,
      ProviderPaymentId = "payment-2",
      Result = ReceiptResult.COMPLETED
    };

    // Act
    var result = await _service.ProcessAsync(
        request,
        CancellationToken.None);

    // Assert
    result.IsSuccess.Should().BeFalse();

    _unitOfWork.Verify(
        x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
        Times.Never);
  }

  [Fact]
  public async Task ProcessAsync_ShouldCompleteOperation()
  {
    // Arrange
    var operation = Operation.Create(
        "operation-1",
        100,
        "RUB",
        "test");

    operation.WaitForReceipt(
        _stateMachine,
        "payment-1");

    _repository
        .Setup(x => x.GetByIdAsync(
            operation.Id,
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(operation);

    var request = new ReceiptRequest
    {
      OperationId = operation.Id,
      ProviderPaymentId = "payment-1",
      Result = ReceiptResult.COMPLETED
    };

    // Act
    var result = await _service.ProcessAsync(
        request,
        CancellationToken.None);

    // Assert
    result.IsSuccess.Should().BeTrue();

    operation.Status.Should().Be(OperationStatus.Completed);

    _unitOfWork.Verify(
        x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
        Times.Once);
  }

  [Fact]
  public async Task ProcessAsync_ShouldRejectOperation()
  {
    // Arrange
    var operation = Operation.Create(
        "operation-1",
        100,
        "RUB",
        "test");

    operation.WaitForReceipt(
        _stateMachine,
        "payment-1");

    _repository
        .Setup(x => x.GetByIdAsync(
            operation.Id,
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(operation);

    var request = new ReceiptRequest
    {
      OperationId = operation.Id,
      ProviderPaymentId = "payment-1",
      Result = ReceiptResult.REJECTED
    };

    // Act
    var result = await _service.ProcessAsync(
        request,
        CancellationToken.None);

    // Assert
    result.IsSuccess.Should().BeTrue();

    operation.Status.Should().Be(OperationStatus.Rejected);

    _unitOfWork.Verify(
        x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
        Times.Once);
  }
}
