using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using QuanLyTaiKhoanAPI.Entities;

namespace QuanLyTaiKhoanAPI.Services;

public sealed record TowerLevelRules(int Level, int QuestionGrade, int RequiredQuestions, int AdvancedQuestions)
{
    public int NormalQuestions => RequiredQuestions - AdvancedQuestions;
}

public sealed record TowerQuestionAvailability(int NormalQuestions, int AdvancedQuestions)
{
    public bool CanStart(TowerLevelRules rules) =>
        NormalQuestions >= rules.NormalQuestions && AdvancedQuestions >= rules.AdvancedQuestions;

    public string? UnavailableReason(TowerLevelRules rules) => CanStart(rules) ? null :
        $"Tầng {rules.Level} cần {rules.NormalQuestions} câu Thường và {rules.AdvancedQuestions} câu Nâng cao lớp {rules.QuestionGrade}. " +
        $"Sau khi loại câu trùng với tầng liền kề, hiện có {NormalQuestions} câu Thường và {AdvancedQuestions} câu Nâng cao phù hợp.";
}

public static class TowerQuestionPolicy
{
    public const int LevelCount = 1000;
    public const string Normal = "Thường";
    public const string Advanced = "Nâng cao";
    private static readonly TowerLevelRules[] Rules = BuildRules();

    public static TowerLevelRules GetRules(int level) => Rules[Math.Clamp(level, 1, LevelCount) - 1];

    private static TowerLevelRules[] BuildRules()
    {
        var result = new TowerLevelRules[LevelCount];
        for (var level = 1; level <= LevelCount; level++)
        {
            var grade = 5 + (level - 1) * 7 / (LevelCount - 1);
            var count = 5 + (level - 1) * 15 / (LevelCount - 1);
            var target = (int)Math.Round((decimal)(level - 1) * count / (LevelCount - 1), MidpointRounding.AwayFromZero);
            // Adding a question must never dilute the preceding level's advanced proportion.
            var previous = level == 1 ? null : result[level - 2];
            var minimum = previous == null ? 0 :
                (previous.AdvancedQuestions * count + previous.RequiredQuestions - 1) / previous.RequiredQuestions;
            result[level - 1] = new(level, grade, count, Math.Max(target, minimum));
        }
        return result;
    }

    public static string ContentKey(string content) => Regex.Replace(
        content.Normalize(NormalizationForm.FormC).Trim(), @"\s+", " ").ToUpperInvariant();

    public static int QuestionGroup(string content) =>
        SHA256.HashData(Encoding.UTF8.GetBytes(ContentKey(content)))[0] & 1;

    public static T[] Shuffle<T>(IEnumerable<T> values, Random? random = null)
    {
        var result = values.ToArray();
        (random ?? Random.Shared).Shuffle(result);
        return result;
    }
}

/// <summary>Stable, content-based groups keep adjacent levels disjoint across retries and bank additions.</summary>
public sealed class TowerQuestionPool
{
    private readonly Dictionary<(int Grade, int Group, string Difficulty), QuizQuestion[]> _groups;

    public TowerQuestionPool(IEnumerable<QuizQuestion> questions)
    {
        _groups = questions.Where(q => q.IsActive && q.Difficulty is TowerQuestionPolicy.Normal or TowerQuestionPolicy.Advanced)
            .GroupBy(q => (q.Grade, Key: TowerQuestionPolicy.ContentKey(q.Content)))
            .Select(g => g.OrderByDescending(q => q.Difficulty == TowerQuestionPolicy.Advanced).ThenBy(q => q.Id).First())
            .GroupBy(q => (q.Grade, Group: TowerQuestionPolicy.QuestionGroup(q.Content), q.Difficulty))
            .ToDictionary(g => g.Key, g => g.ToArray());
    }

    private QuizQuestion[] Questions(TowerLevelRules rules, string difficulty) =>
        _groups.GetValueOrDefault((rules.QuestionGrade, rules.Level & 1, difficulty)) ?? [];

    public TowerQuestionAvailability Availability(TowerLevelRules rules) => new(
        Questions(rules, TowerQuestionPolicy.Normal).Length, Questions(rules, TowerQuestionPolicy.Advanced).Length);

    public QuizQuestion[] Select(TowerLevelRules rules, Random? random = null)
    {
        var availability = Availability(rules);
        if (!availability.CanStart(rules)) throw new InvalidOperationException(availability.UnavailableReason(rules));
        var normal = TowerQuestionPolicy.Shuffle(Questions(rules, TowerQuestionPolicy.Normal), random).Take(rules.NormalQuestions);
        var advanced = TowerQuestionPolicy.Shuffle(Questions(rules, TowerQuestionPolicy.Advanced), random).Take(rules.AdvancedQuestions);
        return TowerQuestionPolicy.Shuffle(normal.Concat(advanced), random);
    }
}
