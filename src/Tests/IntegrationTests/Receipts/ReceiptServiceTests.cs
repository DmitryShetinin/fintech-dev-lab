 
using Application.Abstractions.Telemetry;
using Application.Interface;
using Application.Receipts;
using Application.Receipts.Requests;
using Core.Enums;
using Core.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Application.Tests.Receipts;

public sealed class ReceiptServiceTests
{
    private readonly Mock<IOperationRepository> _repository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ILogger<ReceiptService>> _logger = new();
    private readonly Mock<IOperationMetrics> _metrics = new();

    private readonly OperationStateMachine _stateMachine = new();

    private readonly ReceiptService _service;

    public ReceiptServiceTests()
    {
        _unitOfWork
            .Setup(x => x.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>(
                async (action, cancellationToken) =>
                    await action(cancellationToken));

        _service = new ReceiptService(
            _repository.Object,
            _unitOfWork.Object,
            _stateMachine,
            _logger.Object,
            _metrics.Object);
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
            x => x.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessAsync_ShouldIgnoreDuplicateReceipt_WhenOperationAlreadyCompleted()
    {
        // Arrange
        var operation = Operation.Create(
            "operation-1",
            100,
            "RUB",
            "test",
            PaymentProvider.YooKassa);

        _stateMachine.StartProcessing(operation);
        operation.SetProviderPaymentId("payment-1");
        _stateMachine.Complete(operation);

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

        operation.Status
            .Should()
            .Be(OperationStatus.Completed);

        operation.ProviderPaymentId
            .Should()
            .Be("payment-1");

        _unitOfWork.Verify(
            x => x.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessAsync_ShouldIgnoreDuplicateReceipt_WhenOperationAlreadyRejected()
    {
        // Arrange
        var operation = Operation.Create(
            "operation-1",
            100,
            "RUB",
            "test",
            PaymentProvider.YooKassa);

        _stateMachine.StartProcessing(operation);
        operation.SetProviderPaymentId("payment-1");
        _stateMachine.Reject(operation);

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

        operation.Status
            .Should()
            .Be(OperationStatus.Rejected);

        operation.ProviderPaymentId
            .Should()
            .Be("payment-1");

        _unitOfWork.Verify(
            x => x.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()),
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
            "test",
            PaymentProvider.YooKassa);

        _stateMachine.StartProcessing(operation);

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

        operation.Status
            .Should()
            .Be(OperationStatus.Completed);

        operation.ProviderPaymentId
            .Should()
            .Be("payment-1");

        _unitOfWork.Verify(
            x => x.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()),
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
            "test",
            PaymentProvider.YooKassa);

        _stateMachine.StartProcessing(operation);

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

        operation.Status
            .Should()
            .Be(OperationStatus.Rejected);

        operation.ProviderPaymentId
            .Should()
            .Be("payment-1");

        _unitOfWork.Verify(
            x => x.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
 
 
public async Task ProcessAsync_ShouldReturnFailure_WhenProviderPaymentIdDoesNotMatch()
{
    // Arrange
    var operation = Operation.Create(
        "operation-1",
        100,
        "RUB",
        "test",
        PaymentProvider.YooKassa);

    _stateMachine.StartProcessing(operation);

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

    operation.Status
        .Should()
        .Be(OperationStatus.Processing);

    operation.ProviderPaymentId
        .Should()
        .Be("payment-1");

    _unitOfWork.Verify(
        x => x.ExecuteInTransactionAsync(
            It.IsAny<Func<CancellationToken, Task>>(),
            It.IsAny<CancellationToken>()),
        Times.Never);
}
 

}
 
