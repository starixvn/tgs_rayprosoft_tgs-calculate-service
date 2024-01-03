using Microsoft.EntityFrameworkCore;
using Tgs.Calculate.Repositories;
using Tgs.Calculate.Repositories.Entities;
using Tgs.CalculateWorker;
using Tgs.CalculateWorker.Repositories;

namespace CalculateWorker.Repositories.UnitTests;

public class TgsRepositoryTests
{
    [Fact]
    public async Task TgsRepository_取得最新計算值_如果log有值會使用最新的log()
    {
        // Arrange
        var dbContextOptions = new DbContextOptionsBuilder<TgsContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var dbContext = new TgsContext(dbContextOptions);

        var sut = new CommonTgsRepository(dbContext);

        var calculateId = Guid.NewGuid();
        var changeId = Guid.NewGuid();

        _ = await dbContext.CalculateChangeLogs.AddAsync(new CalculateChangeLog
        {
            Id = changeId,
            CalculateId = calculateId,
            Balance = 100M,
            Change = 0M,
            RecordedOn = DateTime.UtcNow
        });

        _ = await dbContext.SaveChangesAsync();

        var expected = (
            Balance: 100M,
            SnapshotId: changeId);

        // Act
        var actual = await sut.GetBalanceAsync(calculateId);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task TgsRepository_取得最新計算值_如果快照是最新的會使用快照()
    {
        // Arrange
        var dbContextOptions = new DbContextOptionsBuilder<TgsContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var dbContext = new TgsContext(dbContextOptions);

        var sut = new CommonTgsRepository(dbContext);

        var calculateId = Guid.NewGuid();
        var snapshotId = Guid.NewGuid();

        _ = await dbContext.Calculates.AddAsync(new Calculate
        {
            Id = calculateId,
            SnapshotId = snapshotId,
            Balance = 100M
        });

        _ = await dbContext.SaveChangesAsync();

        var expected = (
            Balance: 100M,
            SnapshotId: snapshotId);

        // Act
        var actual = await sut.GetBalanceAsync(calculateId);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task TgsRepository_取得最新計算值_會優先使用最新的log資料()
    {
        // Arrange
        var dbContextOptions = new DbContextOptionsBuilder<TgsContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var dbContext = new TgsContext(dbContextOptions);

        var sut = new CommonTgsRepository(dbContext);

        var calculateId = Guid.NewGuid();
        var snapshotId = Guid.Parse("00ac01bf-7964-4086-b5b5-08d92b1841ba");
        var changeId = Guid.Parse("0114ea80-e596-4a7e-9399-3489ad2880c9");

        _ = await dbContext.CalculateChangeLogs.AddAsync(new CalculateChangeLog
        {
            Id = changeId,
            CalculateId = calculateId,
            Balance = 100M,
            Change = 0M,
            RecordedOn = DateTime.UtcNow
        });

        _ = await dbContext.Calculates.AddAsync(new Calculate
        {
            Id = calculateId,
            SnapshotId = snapshotId,
            Balance = 100M
        });

        _ = await dbContext.SaveChangesAsync();

        var expected = (
            Balance: 100M,
            SnapshotId: changeId);

        // Act
        var actual = await sut.GetBalanceAsync(calculateId);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task TgsRepository_取得最新計算值_如果都沒有資料會傳回Null()
    {
        // Arrange
        var dbContextOptions = new DbContextOptionsBuilder<TgsContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var dbContext = new TgsContext(dbContextOptions);

        var sut = new CommonTgsRepository(dbContext);

        var calculateId = Guid.NewGuid();

        // Act
        var actual = await sut.GetBalanceAsync(calculateId);

        // Assert
        Assert.Null(actual);
    }
}
