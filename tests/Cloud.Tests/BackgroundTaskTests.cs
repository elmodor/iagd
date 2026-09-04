using EvilsoftCommons.Cloud;
using Xunit;

namespace Cloud.Tests;

public class BackgroundTaskTests {
    [Fact]
    public void BackgroundTask_CallsCloudBackupUpdate() {
        using var updateCalled = new ManualResetEventSlim(false);
        var backup = new TestCloudBackup(() => { updateCalled.Set(); });
        using (var task = new BackgroundTask(backup)) {
            var called = updateCalled.Wait(TimeSpan.FromSeconds(2));
            Assert.True(called, "BackgroundTask did not call ICloudBackup.Update() within 2 seconds.");
        }
    }

    private sealed class TestCloudBackup : ICloudBackup {
        private readonly Action _update;
        public TestCloudBackup(Action update) {
            _update = update;
        }
        public void Update() {
            _update();
        }
    }
}
