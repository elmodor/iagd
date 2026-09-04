using Xunit;

namespace Parser.Tests;

public sealed class RequiresItemsArcFactAttribute : FactAttribute {
    public RequiresItemsArcFactAttribute() {
        var itemArcPath = Path.Combine(AppContext.BaseDirectory, "TestData", "Items.arc");
        if (!File.Exists(itemArcPath)) {
            Skip = "Items.arc was not found.";
        }
    }
}
