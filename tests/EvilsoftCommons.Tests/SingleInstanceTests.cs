using Xunit;

namespace EvilsoftCommons.Tests;

public class SingleInstanceTests {
    [Fact]
    public void FirstInstance_IsMarkedAsFirstInstance() {
        var instanceId = Guid.NewGuid();
        using var instance = new global::EvilsoftCommons.SingleInstance.SingleInstance(instanceId);
        Assert.True(instance.IsFirstInstance);
    }

    [Fact]
    public void SecondInstance_IsNotMarkedAsFirstInstance() {
        var instanceId = Guid.NewGuid();
        using var instance1 = new global::EvilsoftCommons.SingleInstance.SingleInstance(instanceId);
        using var instance2 = new global::EvilsoftCommons.SingleInstance.SingleInstance(instanceId);
        Assert.True(instance1.IsFirstInstance);
        Assert.False(instance2.IsFirstInstance);
    }

    [Fact]
    public void SecondInstance_CanPassArgumentsToFirstInstance() {
        var instanceId = Guid.NewGuid();
        using var instance1 = new global::EvilsoftCommons.SingleInstance.SingleInstance(instanceId);
        using var instance2 = new global::EvilsoftCommons.SingleInstance.SingleInstance(instanceId);
        var arguments = new[] { "test", "argument" };
        var passed = instance2.PassArgumentsToFirstInstance(arguments);
        Assert.False(passed);
    }
}
