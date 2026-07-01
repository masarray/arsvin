using AR.Iec61850.SampledValues;
using Xunit;

namespace ARSVIN.Tests.SampledValues;

public sealed class TxTimingHealthTests
{
    [Fact]
    public void RecordsTargetRateAndJitter()
    {
        var health = new TxTimingHealth(targetFramesPerSecond: 1000, timestampFrequency: 1_000_000);

        health.Record(scheduledTicks: 0, sendStartTicks: 0, sendEndTicks: 10);
        health.Record(scheduledTicks: 1_000, sendStartTicks: 1_020, sendEndTicks: 1_035);
        health.Record(scheduledTicks: 2_000, sendStartTicks: 2_100, sendEndTicks: 2_120);

        var snapshot = health.Snapshot(nowTicks: 3_000);

        Assert.Equal(3, snapshot.FrameCount);
        Assert.Equal(1000, snapshot.TargetFramesPerSecond);
        Assert.Equal(1000, snapshot.TargetIntervalMicroseconds);
        Assert.Equal(80, snapshot.MaxAbsJitterMicroseconds);
        Assert.Equal(20, snapshot.MaxSendDurationMicroseconds);
        Assert.Equal(100, snapshot.MaxLateByMicroseconds);
    }

    [Fact]
    public void MarksMissedScheduleAsBad()
    {
        var health = new TxTimingHealth(targetFramesPerSecond: 1000, timestampFrequency: 1_000_000);

        for (var i = 0; i < 8; i++)
        {
            var scheduled = i * 1_000;
            var sendStart = i == 7 ? scheduled + 1_300 : scheduled;
            health.Record(scheduled, sendStart, sendStart + 5);
        }

        var snapshot = health.Snapshot(nowTicks: 10_000);

        Assert.True(snapshot.MissedScheduleCount >= 1);
        Assert.Equal(TxTimingHealthStatus.Bad, snapshot.Status);
    }

    [Fact]
    public void CleanPublisherTimingIsGood()
    {
        var health = new TxTimingHealth(targetFramesPerSecond: 1600, timestampFrequency: 1_600_000);

        for (var i = 0; i < 32; i++)
        {
            var scheduled = i * 1_000;
            health.Record(scheduled, scheduled, scheduled + 8);
        }

        var snapshot = health.Snapshot(nowTicks: 32_000);

        Assert.Equal(TxTimingHealthStatus.Good, snapshot.Status);
        Assert.Equal(0, snapshot.LateFrameCount);
        Assert.Equal(0, snapshot.MissedScheduleCount);
    }
}
