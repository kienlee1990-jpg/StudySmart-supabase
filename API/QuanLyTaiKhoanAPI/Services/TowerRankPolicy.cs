using QuanLyTaiKhoanAPI.Entities;

namespace QuanLyTaiKhoanAPI.Services;

public sealed record TowerRankOutcome(int CurrentLevel, int ConsecutiveFailures, bool Demoted, bool CountsForRank);

public static class TowerRankPolicy
{
    public static TowerRankOutcome Apply(TowerRank rank, int level, int? levelAtStart, int? epochAtStart, bool completed)
    {
        // Replays and sessions from an older rank cannot affect the current streak.
        if (level != rank.CurrentLevel || levelAtStart != level || epochAtStart != rank.Epoch)
            return new(rank.CurrentLevel, rank.ConsecutiveFailures, false, false);
        var previous = rank.CurrentLevel;
        rank.Revision++;
        if (completed)
        {
            rank.CurrentLevel = Math.Min(1000, rank.CurrentLevel + 1);
            rank.ConsecutiveFailures = 0;
            rank.Epoch++;
        }
        else if (++rank.ConsecutiveFailures >= 3)
        {
            rank.CurrentLevel = Math.Max(1, rank.CurrentLevel - 1);
            rank.ConsecutiveFailures = 0;
            rank.Epoch++;
        }
        return new(rank.CurrentLevel, rank.ConsecutiveFailures, rank.CurrentLevel < previous, true);
    }
}
