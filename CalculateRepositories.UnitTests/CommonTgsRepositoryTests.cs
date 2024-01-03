using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Tgs.Calculate;
using Tgs.Calculate.Repositories;
using Tgs.Calculate.Repositories.Entities;

namespace CalculateRepositories.UnitTests;

public class CommonTgsRepositoryTests
{
    [Fact]
    public async Task CommonTgsRepository_取得最新計算值_如果log有值會使用最新的log()
    {
        // Arrange
        var dbContextOptions = new DbContextOptionsBuilder<TgsContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var dbContext = new TgsContext(dbContextOptions);

        var sut = new CommonTgsRepository(dbContext, NullLogger<CommonTgsRepository>.Instance);

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
    public async Task CommonTgsRepository_取得最新計算值_如果快照是最新的會使用快照()
    {
        // Arrange
        var dbContextOptions = new DbContextOptionsBuilder<TgsContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var dbContext = new TgsContext(dbContextOptions);

        var sut = new CommonTgsRepository(dbContext, NullLogger<CommonTgsRepository>.Instance);

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
    public async Task CommonTgsRepository_取得最新計算值_會優先使用最新的log資料()
    {
        // Arrange
        var dbContextOptions = new DbContextOptionsBuilder<TgsContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var dbContext = new TgsContext(dbContextOptions);

        var sut = new CommonTgsRepository(dbContext, NullLogger<CommonTgsRepository>.Instance);

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
    public async Task CommonTgsRepository_取得最新計算值_如果都沒有資料會傳回Null()
    {
        // Arrange
        var dbContextOptions = new DbContextOptionsBuilder<TgsContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var dbContext = new TgsContext(dbContextOptions);

        var sut = new CommonTgsRepository(dbContext, NullLogger<CommonTgsRepository>.Instance);

        var calculateId = Guid.NewGuid();

        // Act
        var actual = await sut.GetBalanceAsync(calculateId);

        // Assert
        Assert.Null(actual);
    }

    [Fact]
    public async Task CommonTgsRepository_將計算的Log存到資料庫中()
    {
        // Arrange
        var dbContextOptions = new DbContextOptionsBuilder<TgsContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var dbContext = new TgsContext(dbContextOptions);

        var sut = new CommonTgsRepository(dbContext, NullLogger<CommonTgsRepository>.Instance);

        var calculateId = Guid.NewGuid();

        var records = new[]{
            new ExchangeLog(
                Guid.NewGuid(),
                calculateId,
                0M,
                0M,
                DateTime.UtcNow)
        };

        // Act
        await sut.InsertExchangeLogsAsync(records);
        var actual = await dbContext.CalculateChangeLogs.ToArrayAsync();

        // Assert
        Assert.Single(actual);
        Assert.Equal(records[0].Id, actual.First().Id);
        Assert.Equal(records[0].Balance, actual.First().Balance);
    }

    [Fact]
    public async Task CommonTgsRepository_將計算的Log過濾已經重複的後存到資料()
    {
        // Arrange
        var dbContextOptions = new DbContextOptionsBuilder<TgsContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var dbContext = new TgsContext(dbContextOptions);

        var sut = new CommonTgsRepository(dbContext, NullLogger<CommonTgsRepository>.Instance);

        var calculateId = Guid.NewGuid();

        var records = new[]{
            new ExchangeLog(
                Guid.NewGuid(),
                calculateId,
                0M,
                0M,
                DateTime.UtcNow),
            new ExchangeLog(
                Guid.NewGuid(),
                calculateId,
                100M,
                100M,
                DateTime.UtcNow)
        };

        await dbContext.CalculateChangeLogs.AddAsync(new CalculateChangeLog
        {
            Id = records[0].Id,
            Balance = records[0].Balance,
            CalculateId = calculateId,
            Change = records[0].Change,
            RecordedOn = records[0].RecordedOn
        });
        await dbContext.SaveChangesAsync();

        // Act
        await sut.MigrateExchangeLogsAsync(records);
        var actual = await dbContext.CalculateChangeLogs.ToArrayAsync();

        // Assert
        Assert.Equal(2, actual.Length);
    }

    [Fact]
    public async Task CommonTgsRepository_更新計算快照值()
    {
        // Arrange
        var dbContextOptions = new DbContextOptionsBuilder<TgsContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var dbContext = new TgsContext(dbContextOptions);

        var sut = new CommonTgsRepository(dbContext, NullLogger<CommonTgsRepository>.Instance);

        var calculateId = Guid.NewGuid();

        await dbContext.CalculateChangeLogs.AddRangeAsync([
            new CalculateChangeLog
            {
                Id = Guid.Parse("007df1e7-3b27-491a-8cc1-51e37ef6bc5a"),
                Balance = 0M,
                CalculateId = calculateId,
                Change = 0M,
                RecordedOn = DateTime.UtcNow
            },
            new CalculateChangeLog
            {
                Id = Guid.Parse("0087851e-54ec-4b29-91fa-d93e6ad4b2c4"),
                Balance = 100M,
                CalculateId = calculateId,
                Change = 100M,
                RecordedOn = DateTime.UtcNow
            }]);

        _ = await dbContext.Calculates.AddAsync(new Calculate
        {
            Id = calculateId,
            Balance = 0M,
            SnapshotId = Guid.Parse("007df1e7-3b27-491a-8cc1-51e37ef6bc5a")
        });

        await dbContext.SaveChangesAsync();

        // Act
        await sut.UpdateSnapshotAsync(calculateId);
        var actual = await dbContext.Calculates.FirstAsync();

        // Assert
        Assert.Equal(100M, actual.Balance);
    }

    [Fact]
    public async Task CommonTgsRepository_更新計算快照值_如果還沒有建立快照會新增新的快照()
    {
        // Arrange
        var dbContextOptions = new DbContextOptionsBuilder<TgsContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var dbContext = new TgsContext(dbContextOptions);

        var sut = new CommonTgsRepository(dbContext, NullLogger<CommonTgsRepository>.Instance);

        var calculateId = Guid.NewGuid();

        await dbContext.CalculateChangeLogs.AddRangeAsync([
            new CalculateChangeLog
            {
                Id = Guid.Parse("007df1e7-3b27-491a-8cc1-51e37ef6bc5a"),
                Balance = 0M,
                CalculateId = calculateId,
                Change = 0M,
                RecordedOn = DateTime.UtcNow
            },
            new CalculateChangeLog
            {
                Id = Guid.Parse("0087851e-54ec-4b29-91fa-d93e6ad4b2c4"),
                Balance = 100M,
                CalculateId = calculateId,
                Change = 100M,
                RecordedOn = DateTime.UtcNow
            }]);

        await dbContext.SaveChangesAsync();

        // Act
        await sut.UpdateSnapshotAsync(calculateId);
        var actual = await dbContext.Calculates.FirstAsync();

        // Assert
        Assert.Equal(100M, actual.Balance);
    }

    [Fact]
    public async Task CommonTgsRepository_更新計算快照值_如果沒有新的資料不會變更快照值()
    {
        // Arrange
        var dbContextOptions = new DbContextOptionsBuilder<TgsContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var dbContext = new TgsContext(dbContextOptions);

        var sut = new CommonTgsRepository(dbContext, NullLogger<CommonTgsRepository>.Instance);

        var calculateId = Guid.NewGuid();

        await dbContext.CalculateChangeLogs.AddRangeAsync([
            new CalculateChangeLog
            {
                Id = Guid.Parse("007df1e7-3b27-491a-8cc1-51e37ef6bc5a"),
                Balance = 0M,
                CalculateId = calculateId,
                Change = 0M,
                RecordedOn = DateTime.UtcNow
            },
            new CalculateChangeLog
            {
                Id = Guid.Parse("0087851e-54ec-4b29-91fa-d93e6ad4b2c4"),
                Balance = 100M,
                CalculateId = calculateId,
                Change = 100M,
                RecordedOn = DateTime.UtcNow
            }]);

        _ = await dbContext.Calculates.AddAsync(new Calculate
        {
            Id = calculateId,
            Balance = 100M,
            SnapshotId = Guid.Parse("0087851e-54ec-4b29-91fa-d93e6ad4b2c4")
        });

        await dbContext.SaveChangesAsync();

        // Act
        await sut.UpdateSnapshotAsync(calculateId);
        var actual = await dbContext.Calculates.FirstAsync();

        // Assert
        Assert.Equal(100M, actual.Balance);
    }

    [Fact]
    public async Task CommonTgsRepository_更新計算快照值_如果沒有查詢到資料不會建行快照()
    {
        // Arrange
        var dbContextOptions = new DbContextOptionsBuilder<TgsContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var dbContext = new TgsContext(dbContextOptions);

        var sut = new CommonTgsRepository(dbContext, NullLogger<CommonTgsRepository>.Instance);

        var calculateId = Guid.NewGuid();

        // Act
        await sut.UpdateSnapshotAsync(calculateId);
        var actual = await dbContext.Calculates.CountAsync();

        // Assert
        Assert.Equal(0, actual);
    }
}
