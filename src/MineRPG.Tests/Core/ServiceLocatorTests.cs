using System;

using FluentAssertions;

using MineRPG.Core.DI;

namespace MineRPG.Tests.Core;

public sealed class ServiceLocatorTests : IDisposable
{
    private readonly ServiceLocator _locator = new();

    public void Dispose() => ServiceLocator.ResetInstance();

    [Fact]
    public void Register_AndGet_ReturnsInstance()
    {
        // Arrange
        TestService service = new TestService();

        // Act
        _locator.Register<ITestService>(service);

        // Assert
        _locator.Get<ITestService>().Should().BeSameAs(service);
    }

    [Fact]
    public void Register_Duplicate_ThrowsInvalidOperation()
    {
        // Arrange
        _locator.Register<ITestService>(new TestService());

        // Act
        Action act = () => _locator.Register<ITestService>(new TestService());

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*already registered*");
    }

    [Fact]
    public void Replace_OverwritesExisting()
    {
        // Arrange
        TestService first = new TestService();
        TestService second = new TestService();
        _locator.Register<ITestService>(first);

        // Act
        _locator.Replace<ITestService>(second);

        // Assert
        _locator.Get<ITestService>().Should().BeSameAs(second);
    }

    [Fact]
    public void Replace_WhenNotRegistered_RegistersNew()
    {
        // Arrange
        TestService service = new TestService();

        // Act
        _locator.Replace<ITestService>(service);

        // Assert
        _locator.Get<ITestService>().Should().BeSameAs(service);
    }

    [Fact]
    public void Get_WhenNotRegistered_ThrowsInvalidOperation()
    {
        // Act
        Action act = () => _locator.Get<ITestService>();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*has not been registered*");
    }

    [Fact]
    public void TryGet_WhenRegistered_ReturnsTrueAndService()
    {
        // Arrange
        TestService service = new TestService();
        _locator.Register<ITestService>(service);

        // Act
        bool isFound = _locator.TryGet<ITestService>(out ITestService? result);

        // Assert
        isFound.Should().BeTrue();
        result.Should().BeSameAs(service);
    }

    [Fact]
    public void TryGet_WhenNotRegistered_ReturnsFalse()
    {
        // Act
        bool isFound = _locator.TryGet<ITestService>(out ITestService? result);

        // Assert
        isFound.Should().BeFalse();
        result.Should().BeNull();
    }

    [Fact]
    public void Register_Null_ThrowsArgumentNull()
    {
        // Act
        Action act = () => _locator.Register<ITestService>(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Replace_Null_ThrowsArgumentNull()
    {
        // Act
        Action act = () => _locator.Replace<ITestService>(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Instance_WhenNotSet_ThrowsInvalidOperation()
    {
        // Arrange
        ServiceLocator.ResetInstance();

        // Act
        Action act = () => _ = ServiceLocator.Instance;

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*has not been initialized*");
    }

    [Fact]
    public void SetInstance_AndInstance_ReturnsLocator()
    {
        // Act
        ServiceLocator.SetInstance(_locator);

        // Assert
        ServiceLocator.Instance.Should().BeSameAs(_locator);
    }

    private interface ITestService { }
    private sealed class TestService : ITestService { }
}
