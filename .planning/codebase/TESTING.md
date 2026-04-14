# Testing Patterns

**Analysis Date:** 2026-04-10

## Test Framework

**Runner:**
- No test framework detected
- Checked csproj: No `xunit`, `nunit`, `mstest` packages referenced
- No test projects (.Tests.csproj) in repository
- No test files (.cs files with Test/Spec naming) found

**Assertion Library:**
- Not applicable - no testing infrastructure present

**Run Commands:**
```bash
# Build project
dotnet build

# Run application (no automated tests)
dotnet run
```

## Test File Organization

**Current State:**
- Zero test files
- Zero test coverage
- No test directory

**Recommended Structure (for future implementation):**
```
stardew_medieval_v3.Tests/
├── Unit/
│   ├── src/Core/
│   │   ├── TimeManagerTests.cs
│   │   ├── InputManagerTests.cs
│   │   ├── SaveManagerTests.cs
│   │   └── GameStateTests.cs
│   ├── src/Farming/
│   │   ├── GridManagerTests.cs
│   │   ├── CropManagerTests.cs
│   │   ├── CropInstanceTests.cs
│   │   └── CropDataTests.cs
│   ├── src/Player/
│   │   ├── PlayerEntityTests.cs
│   │   └── PlayerStatsTests.cs
│   └── src/World/
│       └── TileMapTests.cs
├── Fixtures/
│   ├── CropDataFixtures.cs
│   ├── GameStateFixtures.cs
│   └── TestHelpers.cs
└── stardew_medieval_v3.Tests.csproj
```

**Naming Convention (to establish):**
- Test class: `[ClassUnderTest]Tests.cs` - e.g., `TimeManagerTests.cs`
- Test method: `[Method]_[Scenario]_[ExpectedResult]` - e.g., `Update_TimeAdvances_IncreasesGameTime()`

## Test Structure

**Recommended Pattern (xUnit):**
```csharp
public class TimeManagerTests
{
    [Fact]
    public void Update_TimeAdvances_IncreasesGameTime()
    {
        // Arrange
        var timeManager = new TimeManager { DayDurationSeconds = 120f };
        float initialTime = timeManager.GameTime;

        // Act
        timeManager.Update(10f); // 10 seconds elapsed

        // Assert
        Assert.Equal(initialTime + 10f / 120f, timeManager.GameTime);
    }

    [Fact]
    public void Update_DayComplete_TriggersAdvanceAndResetsTime()
    {
        // Arrange
        var timeManager = new TimeManager { DayDurationSeconds = 120f };
        int advanceCount = 0;
        timeManager.OnDayAdvanced += () => advanceCount++;

        // Act
        timeManager.Update(130f); // More than one day

        // Assert
        Assert.Equal(1, advanceCount);
        Assert.True(timeManager.GameTime < 1f);
    }
}
```

**Patterns to Implement:**
- **Arrange-Act-Assert (AAA):** Clear separation of test setup, execution, and verification
- **One assertion per test:** Or grouped assertions on related state (e.g., `Assert.Equal() && Assert.True()`)
- **Fixture-based setup:** For common test data (crop definitions, game state templates)
- **Parametrized tests:** `[Theory]` + `[InlineData]` for testing multiple scenarios

## Mocking

**Framework to Adopt:**
- Moq (most common in .NET)
- Or: Xunit built-in mocking via dependency injection

**Patterns (for future tests):**

**Dependency Injection via Constructor:**
```csharp
// Classes designed for testing:
public class GridManager
{
    private readonly TileMap _map;
    
    public GridManager(TileMap map)
    {
        _map = map; // Can be mocked in tests
    }
}

// Test with mock:
[Fact]
public void TryTill_FarmZone_Success()
{
    // Arrange
    var mockMap = new Mock<TileMap>();
    mockMap.Setup(m => m.IsFarmZone(5, 5)).Returns(true);
    
    var gridManager = new GridManager(mockMap.Object);
    var stats = new PlayerStats { MaxStamina = 100f };

    // Act
    bool result = gridManager.TryTill(new Point(5, 5), stats);

    // Assert
    Assert.True(result);
}
```

**What to Mock:**
- External I/O (file system, network) - e.g., `SaveManager.Load()`
- MonoGame dependencies - `GraphicsDevice`, `Texture2D` (complex to construct)
- Time-dependent behavior - `TimeManager.Update()` can be called with specific deltaTime
- Event subscriptions - Verify `OnDayAdvanced` is invoked

**What NOT to Mock:**
- Domain logic classes - `CropManager`, `GridManager`, `PlayerStats`
- Data objects - `CropData`, `CropInstance`, `CellData`
- Pure functions - collision detection, light calculation
- Internal state transitions

## Fixtures and Factories

**Test Data (to create):**
```csharp
public static class CropDataFixtures
{
    public static CropData CreateDefaultCrop() => new CropData
    {
        Name = "Test Crop",
        StageCount = 7,
        DaysPerStage = 1,
        DaysToWilt = 2,
        SpriteHeight = 16,
        YieldItemName = "Test Item",
        YieldQuantity = 1
    };

    public static CropData CreateFastGrowthCrop() => new CropData
    {
        Name = "Fast Crop",
        StageCount = 3,
        DaysPerStage = 1,
        DaysToWilt = 10,
        SpriteHeight = 16,
        YieldItemName = "Fast Item",
        YieldQuantity = 2
    };
}

public static class GameStateFixtures
{
    public static GameState CreateNewGame() => new GameState
    {
        SaveVersion = 2,
        DayNumber = 1,
        Season = 0,
        StaminaCurrent = 100f,
        PlayerX = 160f,
        PlayerY = 160f,
        GameTime = 0f,
        FarmCells = new()
    };

    public static GameState CreateAdvancedGame() => new GameState
    {
        SaveVersion = 2,
        DayNumber = 10,
        Season = 0,
        StaminaCurrent = 50f,
        PlayerX = 320f,
        PlayerY = 320f,
        GameTime = 0.5f,
        FarmCells = new()
    };
}
```

**Location:**
- `stardew_medieval_v3.Tests/Fixtures/CropDataFixtures.cs`
- `stardew_medieval_v3.Tests/Fixtures/GameStateFixtures.cs`
- `stardew_medieval_v3.Tests/Fixtures/TestHelpers.cs` (utility methods)

## Coverage

**Requirements:** 
- None currently enforced
- Recommended target: 70%+ for domain logic (Core, Farming, Player)
- Lower priority: UI, World (graphics-heavy, hard to test)

**View Coverage:**
```bash
# Using OpenCover (recommended for .NET):
OpenCover.Console.exe -target:dotnet.exe -targetargs:"test" -output:coverage.xml

# Then use ReportGenerator to view:
ReportGenerator.exe -reports:coverage.xml -targetdir:coverage-report
# Open coverage-report/index.html
```

**Tool to adopt:**
- OpenCover + ReportGenerator (free)
- Or: Coverlet + ReportGenerator (more modern)

## Test Types

**Unit Tests:**
- Scope: Single class in isolation
- Approach: Mock/inject dependencies
- Examples to implement:
  - `TimeManager`: Update time, day advance triggers, hour calculation
  - `GridManager`: Tilling, watering, cell state
  - `CropInstance`: Growth progression, wilt logic
  - `PlayerStats`: Stamina spending, restoration
  - `SaveManager`: Save/load round-trip (file I/O testing)

**Integration Tests:**
- Scope: Multiple classes working together
- Approach: No mocks, use real instances
- Examples to implement:
  - `Game1 -> GridManager -> TileMap`: Tilling tiles and checking farm zone rules
  - `CropManager -> GridManager -> CropRegistry`: Planting and growing crops end-to-end
  - `SaveManager <-> GridManager`: Load game state, verify farm cells reconstructed correctly
  - `TimeManager -> CropManager`: Day advance triggers crop growth

**E2E Tests:**
- Not recommended for MonoGame (graphics/input heavy)
- Alternative: Record-playback of game input and verify save state

## Common Patterns to Test

**Async Testing (not applicable):**
- C# MonoGame is synchronous
- No async/await patterns in codebase

**Error Testing:**
```csharp
[Fact]
public void LoadContent_MissingFile_ReturnsNull()
{
    // Arrange
    var texture = CropRegistry.LoadTexture(device, "NonExistent/Path.png");

    // Assert
    Assert.Null(texture);
}

[Fact]
public void TryTill_InsufficientStamina_ReturnsFalse()
{
    // Arrange
    var gridManager = new GridManager(map);
    var stats = new PlayerStats { MaxStamina = 2f }; // Less than 5 needed
    stats.TrySpendStamina(1f); // Leave 1 stamina

    // Act
    bool result = gridManager.TryTill(new Point(0, 0), stats);

    // Assert
    Assert.False(result);
    Assert.Equal(1f, stats.CurrentStamina); // Unchanged
}
```

**State Transition Testing:**
```csharp
[Fact]
public void OnDayAdvanced_CropRipe_StartsWiltTracking()
{
    // Arrange
    var cropData = new CropData 
    { 
        StageCount = 1, 
        DaysPerStage = 1, 
        DaysToWilt = 2 
    };
    var crop = new CropInstance(cropData);
    
    // Act: Advance to ripeness
    crop.AdvanceDay(); // Now ripe at day 1
    
    // Assert
    Assert.True(crop.IsRipe);
    Assert.False(crop.IsWilted);
    
    // Act: Advance past wilt threshold
    crop.AdvanceDay(); // Day 2, threshold hit
    bool wilted = crop.CheckWilt();
    
    // Assert
    Assert.True(wilted);
    Assert.True(crop.IsWilted);
}
```

**Event Invocation Testing:**
```csharp
[Fact]
public void Update_HourPasses_InvokesEvent()
{
    // Arrange
    var timeManager = new TimeManager { DayDurationSeconds = 60f };
    int eventCount = 0;
    timeManager.OnHourPassed += (hour) => eventCount++;

    // Act
    timeManager.Update(3f); // 3 seconds = 0.05 game days = 1 hour (20 hours/day)

    // Assert
    Assert.Equal(1, eventCount);
}
```

## Recommended Test Framework Setup

**Add to csproj:**
```xml
<ItemGroup Condition="'$(Configuration)' == 'Debug'">
    <PackageReference Include="xunit" Version="2.6.0" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.0" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.7.0" />
    <PackageReference Include="Moq" Version="4.20.0" />
    <PackageReference Include="Coverlet.collector" Version="6.0.0" />
</ItemGroup>
```

**Create test project:**
```bash
dotnet new xunit -n stardew_medieval_v3.Tests
dotnet add stardew_medieval_v3.Tests reference ../stardew_medieval_v3/stardew_medieval_v3.csproj

# Run tests:
dotnet test
```

## Current Gaps

**High Priority:**
- No unit tests for `GridManager` - complex tiling/watering logic
- No tests for `CropManager` - lifecycle logic with multiple state transitions
- No tests for `SaveManager` - critical persistence layer

**Medium Priority:**
- No tests for `TimeManager` - event timing is easy to get wrong
- No tests for `PlayerStats` - stamina calculations
- No integration tests for day-cycle flow

**Low Priority:**
- `TileMap` graphics rendering (harder to test, less critical)
- `PlayerEntity` movement/animation (graphics-heavy)
- `HUD` rendering (UI tests are expensive)

---

*Testing analysis: 2026-04-10*
