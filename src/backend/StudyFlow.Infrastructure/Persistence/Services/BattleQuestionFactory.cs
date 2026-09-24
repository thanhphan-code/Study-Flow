using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using StudyFlow.Application.Battles;
using StudyFlow.Domain.Entities;

namespace StudyFlow.Infrastructure.Persistence.Services;

internal static class BattleQuestionFactory
{
    public static readonly string[] Types = ["MultipleChoice", "TrueFalse", "FillBlank", "Matching", "Ordering", "ShortAnswer"];

    public static List<BattleQuestion> Build(BattleRoom room, List<Flashcard> cards, string[] types, List<SourceReference> sources,
        List<Question> quizQuestions, List<AnswerOption> quizOptions)
    {
        var result = new List<BattleQuestion>();
        var fingerprints = new HashSet<string>(StringComparer.Ordinal);
        var unique = cards.DistinctBy(x => Normalize(x.FrontText)).OrderBy(_ => RandomNumberGenerator.GetInt32(int.MaxValue)).ToList();
        var matchingPool = unique.DistinctBy(x => Normalize(x.BackText)).ToList();
        var trueFalseCount = 0;
        foreach (var question in quizQuestions.OrderBy(_ => RandomNumberGenerator.GetInt32(int.MaxValue)))
        {
            if (result.Count == room.QuestionCount) break;
            var type = question.Type.ToString();
            var choices = quizOptions.Where(x => x.QuestionId == question.Id).ToArray();
            if (!types.Contains(type) || question.QuestionText.Length > 1000 || choices.Count(x => x.IsCorrect) != 1
                || choices.Length is < 2 or > 4 || choices.Select(x => Normalize(x.Text)).Distinct().Count() != choices.Length
                || type == "TrueFalse" && trueFalseCount >= room.QuestionCount / 4) continue;
            var fingerprint = Normalize(question.QuestionText + "\n" + choices.Single(x => x.IsCorrect).Text);
            if (!fingerprints.Add(fingerprint)) continue;
            var q = new BattleQuestion { BattleRoomId = room.Id, SourceQuestionId = question.Id, FlashcardId = question.FlashcardId,
                QuestionType = type, Prompt = question.QuestionText, Explanation = question.Explanation, OrderIndex = result.Count,
                TimeLimitSeconds = room.DefaultTimeLimitSeconds, Difficulty = DifficultyFor(type),
                OptionsJson = JsonSerializer.Serialize(Shuffle(choices.Select(x => new BattleOption(x.Id.ToString("N"), x.Text)))),
                CorrectAnswer = choices.Single(x => x.IsCorrect).Id.ToString("N") };
            AttachSource(q, sources.FirstOrDefault(x => x.ContentId == question.Id));
            if (type == "TrueFalse") trueFalseCount++;
            result.Add(q);
        }
        foreach (var card in unique)
        {
            if (result.Count == room.QuestionCount) break;
            var options = cards.Select(x => x.BackText).DistinctBy(Normalize).Where(x => Normalize(x) != Normalize(card.BackText)).OrderBy(_ => RandomNumberGenerator.GetInt32(int.MaxValue)).Take(3).ToList();
            // Ordered content is only taken from an explicitly numbered answer; never invent a semantic order.
            var steps = card.BackText.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var ordered = steps.Length is >= 3 and <= 8 && steps.Select((s, i) => s.StartsWith($"{i + 1}. ") || s.StartsWith($"{i + 1}) ")).All(x => x);
            var available = types.Where(t => (t != "MultipleChoice" || options.Count == 3)
                && (t != "TrueFalse" || options.Count > 0 && trueFalseCount < room.QuestionCount / 4 && card.FrontText.Length + Math.Max(card.BackText.Length, options[0].Length) + 3 <= 1000)
                && (t != "Matching" || matchingPool.Count >= 4)
                && (t is not ("FillBlank" or "ShortAnswer") || card.BackText.Length <= 4000)
                && (t != "Ordering" || ordered)).ToArray();
            if (available.Length == 0) continue;
            var target = room.Difficulty == "Easy" ? 0 : room.Difficulty == "Hard" ? 2 : result.Count % 10 < 3 ? 0 : result.Count % 10 < 8 ? 1 : 2;
            var type = available.OrderBy(t => Math.Abs(DifficultyLevel(DifficultyFor(t)) - target))
                .ThenBy(_ => RandomNumberGenerator.GetInt32(int.MaxValue)).First();
            var q = new BattleQuestion { BattleRoomId = room.Id, FlashcardId = card.Id, QuestionType = type, Prompt = card.FrontText,
                CorrectAnswer = card.BackText, AcceptedAnswers = card.AcceptedAnswers, Explanation = card.Explanation, OrderIndex = result.Count,
                Difficulty = DifficultyFor(type),
                TimeLimitSeconds = room.DefaultTimeLimitSeconds is null ? null : Math.Max(room.DefaultTimeLimitSeconds.Value, type is "ShortAnswer" or "Matching" or "Ordering" ? 30 : 10) };
            if (type == "MultipleChoice")
            {
                options.Add(card.BackText);
                var choices = Shuffle(options.Select(x => new BattleOption(Guid.NewGuid().ToString("N"), x)));
                q.OptionsJson = JsonSerializer.Serialize(choices);
                q.CorrectAnswer = choices.Single(x => x.Text == card.BackText).Id;
            }
            if (type == "TrueFalse")
            {
                trueFalseCount++;
                var correct = RandomNumberGenerator.GetInt32(2) == 0;
                q.Prompt = $"{card.FrontText}\n= { (correct ? card.BackText : options[0]) }";
                q.CorrectAnswer = correct ? "true" : "false";
                q.OptionsJson = JsonSerializer.Serialize(new[] { new BattleOption("true", "Đúng"), new BattleOption("false", "Sai") });
            }
            if (type == "Matching")
            {
                var start = matchingPool.FindIndex(x => Normalize(x.BackText) == Normalize(card.BackText));
                var related = new[] { card }.Concat(Enumerable.Range(1, 3).Select(offset => matchingPool[(start + offset) % matchingPool.Count])).ToArray();
                var left = related.Select((x, i) => new BattleOption($"l{i}", x.FrontText)).ToArray();
                var right = related.Select(x => new BattleOption(Guid.NewGuid().ToString("N"), x.BackText)).ToArray();
                q.Prompt = "Ghép mỗi khái niệm với ý nghĩa tương ứng.";
                q.LeftItemsJson = JsonSerializer.Serialize(left);
                q.OptionsJson = JsonSerializer.Serialize(Shuffle(right));
                q.CorrectAnswer = JsonSerializer.Serialize(right.Select(x => x.Id));
            }
            if (type == "Ordering")
            {
                var items = steps.Select(s => new BattleOption(Guid.NewGuid().ToString("N"), s[(s.IndexOf(' ') + 1)..])).ToArray();
                q.OptionsJson = JsonSerializer.Serialize(Shuffle(items));
                q.CorrectAnswer = JsonSerializer.Serialize(items.Select(x => x.Id));
            }
            var fingerprint = type == "Matching" ? Normalize(string.Join("\n", DisplayAnswer(q).Split('\n').Order(StringComparer.Ordinal))) : Normalize(card.FrontText + "\n" + card.BackText);
            if (DisplayAnswer(q).Length > 5000) continue;
            if (!fingerprints.Add(fingerprint)) continue;
            AttachSource(q, sources.FirstOrDefault(x => x.ContentId == card.Id));
            result.Add(q);
        }
        return result;
    }

    private static string Normalize(string value) => Regex.Replace(value.Trim().Normalize().ToUpperInvariant(), @"\s+", " ");
    // Existing content has no authored difficulty metadata. Estimate retrieval effort by format,
    // rather than relabeling the same question as hard merely because the host chose that preset.
    private static string DifficultyFor(string type) => type is "MultipleChoice" or "TrueFalse" ? "Easy" : type == "FillBlank" ? "Medium" : "Hard";
    private static int DifficultyLevel(string difficulty) => difficulty == "Easy" ? 0 : difficulty == "Hard" ? 2 : 1;
    private static void AttachSource(BattleQuestion q, SourceReference? source)
    {
        if (source is null) return;
        q.SourceReferenceId = source.Id;
        q.SourceJson = JsonSerializer.Serialize(new { name = source.DocumentNameSnapshot, page = source.PageNumber, slide = source.SlideNumber, section = source.SectionTitle, snippet = source.SourceSnippetSnapshot });
    }

    private static T[] Shuffle<T>(IEnumerable<T> values) => values.OrderBy(_ => RandomNumberGenerator.GetInt32(int.MaxValue)).ToArray();
    public static BattleOption[] Options(string json) => JsonSerializer.Deserialize<BattleOption[]>(json)!;
    public static string DisplayAnswer(BattleQuestion q)
    {
        var options = Options(q.OptionsJson);
        if (q.QuestionType is "MultipleChoice" or "TrueFalse") return options.Single(x => x.Id == q.CorrectAnswer).Text;
        if (q.QuestionType is "Matching" or "Ordering")
        {
            var ids = JsonSerializer.Deserialize<string[]>(q.CorrectAnswer)!;
            var left = Options(q.LeftItemsJson);
            return string.Join("\n", ids.Select((id, i) => q.QuestionType == "Matching" ? $"{left[i].Text} → {options.Single(x => x.Id == id).Text}" : $"{i + 1}. {options.Single(x => x.Id == id).Text}"));
        }
        return q.CorrectAnswer;
    }
}
