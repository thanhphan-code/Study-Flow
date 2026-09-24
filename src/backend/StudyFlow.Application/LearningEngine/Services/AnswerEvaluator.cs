using System.Globalization;
using System.Text;
using StudyFlow.Application.LearningEngine.Interfaces;
using StudyFlow.Domain.Entities;
using StudyFlow.Domain.Enums;

namespace StudyFlow.Application.LearningEngine.Services;

public sealed class AnswerEvaluator : IAnswerEvaluator
{
    public LearningAttemptResult EvaluateSequence(IReadOnlyList<string> expected, IReadOnlyList<string> submitted) =>
        expected.Count > 0 && expected.SequenceEqual(submitted, StringComparer.Ordinal) ? LearningAttemptResult.Correct : LearningAttemptResult.Wrong;
    public LearningAttemptResult Evaluate(Flashcard card, RecallDirection direction, string submittedAnswer, LearningAttemptType attemptType)
    {
        var accepted = direction == RecallDirection.Reverse
            ? new[] { card.FrontText }
            : new[] { card.BackText }.Concat((card.AcceptedAnswers ?? string.Empty).Split(['\n', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        var normalized = Normalize(submittedAnswer, false);
        if (accepted.Any(x => Normalize(x, false) == normalized)) return LearningAttemptResult.Correct;
        if (attemptType is LearningAttemptType.MultipleChoice or LearningAttemptType.TrueFalse) return LearningAttemptResult.Wrong;
        var loose = Normalize(submittedAnswer, true);
        return accepted.Any(x => IsClose(loose, Normalize(x, true))) ? LearningAttemptResult.Close : LearningAttemptResult.Wrong;
    }

    private static bool IsClose(string left, string right)
    {
        if (left == right) return true;
        if (left.Length < 3 || right.Length < 3) return false;
        if (1d - (double)Levenshtein(left, right) / Math.Max(left.Length, right.Length) >= .88) return true;
        var expected = right.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        var actual = left.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        return expected.Count >= 5 && (double)expected.Intersect(actual).Count() / expected.Count >= .75;
    }

    private static string Normalize(string value, bool removeDiacritics)
    {
        var source = value.Trim().ToLowerInvariant().Normalize(removeDiacritics ? NormalizationForm.FormD : NormalizationForm.FormC);
        var builder = new StringBuilder(source.Length); var pendingSpace = false;
        foreach (var character in source)
        {
            if (removeDiacritics && CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark) continue;
            var normalized = removeDiacritics && character == 'đ' ? 'd' : character;
            if (char.IsLetterOrDigit(normalized)) { if (pendingSpace && builder.Length > 0) builder.Append(' '); builder.Append(normalized); pendingSpace = false; }
            else pendingSpace = true;
        }
        return builder.ToString();
    }

    private static int Levenshtein(string left, string right)
    {
        var previous = Enumerable.Range(0, right.Length + 1).ToArray();
        for (var row = 1; row <= left.Length; row++)
        {
            var current = new int[right.Length + 1]; current[0] = row;
            for (var column = 1; column <= right.Length; column++)
                current[column] = Math.Min(Math.Min(current[column - 1] + 1, previous[column] + 1), previous[column - 1] + (left[row - 1] == right[column - 1] ? 0 : 1));
            previous = current;
        }
        return previous[right.Length];
    }
}
