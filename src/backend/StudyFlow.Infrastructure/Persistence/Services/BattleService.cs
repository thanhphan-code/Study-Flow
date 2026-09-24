using System.Data;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using StudyFlow.Application.Battles;
using StudyFlow.Application.Common.Models;
using StudyFlow.Application.LearningEngine.DTOs;
using StudyFlow.Application.LearningEngine.Interfaces;
using StudyFlow.Domain.Entities;
using StudyFlow.Domain.Enums;

namespace StudyFlow.Infrastructure.Persistence.Services;

internal sealed class BattleService(StudyFlowDbContext db, ILearningEngine learning, IAnswerEvaluator evaluator, TimeProvider clock, IBattleNotifier notifier) : IBattleService
{
    // Bounded stripes protect the test/in-memory provider. PostgreSQL transaction locks coordinate all API instances.
    private static readonly SemaphoreSlim[] Gates = Enumerable.Range(0, 128).Select(_ => new SemaphoreSlim(1, 1)).ToArray();
    private DateTimeOffset Now => clock.GetUtcNow();
    private static Result<BattleRoomDto> Fail(string message, ErrorType type = ErrorType.Conflict) => Result<BattleRoomDto>.Failure("BATTLE_ERROR", message, type);

    private async Task<Result<BattleRoomDto>> Locked(Guid id, Func<Task<Result<BattleRoomDto>>> work, CancellationToken ct)
    {
        var gate = Gates[(int)((uint)id.GetHashCode() % Gates.Length)];
        await gate.WaitAsync(ct);
        try
        {
            await using var tx = db.Database.ProviderName?.Contains("Npgsql", StringComparison.Ordinal) == true ? await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct) : null;
            if (db.Database.ProviderName?.Contains("Npgsql") == true)
                await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(hashtext({0}))", [$"battle:{id}"], ct);
            var result = await work();
            if (result.IsSuccess)
            {
                await db.SaveChangesAsync(ct);
                if (tx is not null) await tx.CommitAsync(ct);
            }
            return result;
        }
        finally { gate.Release(); }
    }

    public async Task<Result<BattleRoomDto>> CreateAsync(Guid userId, CreateBattleRequest r, CancellationToken ct)
    {
        var types = r.QuestionTypes ?? ["MultipleChoice", "FillBlank", "ShortAnswer"];
        if (string.IsNullOrWhiteSpace(r.Name) || r.Name.Length > 120 || r.QuestionCount is not (10 or 15 or 20 or 30)
            || r.MaxPlayers is not (10 or 20 or 30 or 50) || r.TimeLimitSeconds is not (null or 10 or 15 or 20 or 30)
            || types.Length == 0 || types.Distinct().Count() != types.Length || types.Except(BattleQuestionFactory.Types).Any()
            || r.Mode is not ("Classic" or "Mixed") || r.Difficulty is not ("Easy" or "Balanced" or "Hard")
            || r.LeaderboardMode is not ("Always" or "BetweenRounds" or "EndOnly")
            || r.Mode == "Classic" && types.Any(x => x is "Matching" or "Ordering"))
            return Fail("Cấu hình phòng không hợp lệ.", ErrorType.Validation);
        var set = await db.StudySets.SingleOrDefaultAsync(x => x.Id == r.StudySetId && db.Subjects.Any(s => s.Id == x.SubjectId && s.UserId == userId), ct);
        if (set is null) return Fail("Không tìm thấy bộ học.", ErrorType.NotFound);
        return await Locked(Guid.Empty, async () =>
        {
            if (await db.BattleRooms.CountAsync(x => x.HostUserId == userId && (x.Status == "LobbyOpen" || x.Status == "Locked" || x.Status == "InProgress") && x.ExpiresAt > Now, ct) >= 5)
                return Fail("Bạn đã có 5 phòng đang mở. Hãy kết thúc hoặc hủy phòng cũ.");
            var setIds = await db.StudySetSources.Where(x => x.CombinedStudySetId == set.Id).Select(x => x.SourceStudySetId).ToListAsync(ct);
            setIds.Add(set.Id);
            var cards = await db.Flashcards.Where(x => setIds.Contains(x.StudySetId) && db.StudySets.Any(s => s.Id == x.StudySetId && db.Subjects.Any(subject => subject.Id == s.SubjectId && subject.UserId == userId))).OrderBy(x => x.OrderIndex).Take(2000).ToListAsync(ct);
            const string alphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
            string code;
            do { code = new string(Enumerable.Range(0, 6).Select(_ => alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)]).ToArray()); }
            while (await db.BattleRooms.AnyAsync(x => x.JoinCode == code, ct));
            var room = new BattleRoom { HostUserId = userId, StudySetId = set.Id, Name = r.Name.Trim(), JoinCode = code, MaxPlayers = r.MaxPlayers,
                QuestionCount = r.QuestionCount, DefaultTimeLimitSeconds = r.TimeLimitSeconds, Mode = r.Mode, Difficulty = r.Difficulty,
                LeaderboardMode = r.LeaderboardMode, ExpiresAt = Now.AddHours(6) };
            var quizIds = await db.Quizzes.Where(x => setIds.Contains(x.StudySetId) && db.StudySets.Any(s => s.Id == x.StudySetId && db.Subjects.Any(subject => subject.Id == s.SubjectId && subject.UserId == userId))).Select(x => x.Id).ToListAsync(ct);
            var quizQuestions = await db.Questions.Where(x => quizIds.Contains(x.QuizId)).Take(2000).ToListAsync(ct);
            var questionIds = quizQuestions.Select(x => x.Id).ToList();
            var quizOptions = await db.AnswerOptions.Where(x => questionIds.Contains(x.QuestionId)).ToListAsync(ct);
            var contentIds = cards.Select(x => x.Id).Concat(questionIds).ToList();
            var sources = await db.SourceReferences.Where(x => contentIds.Contains(x.ContentId)).ToListAsync(ct);
            var questions = BattleQuestionFactory.Build(room, cards, types, sources, quizQuestions, quizOptions);
            if (questions.Count < r.QuestionCount) return Fail($"Chỉ có {questions.Count} câu phù hợp. Cần {r.QuestionCount} thẻ khác nhau; trắc nghiệm/ghép cặp cần 4 đáp án khác nhau, sắp xếp cần nội dung đánh số 1., 2., 3. Hãy bổ sung thẻ hoặc chọn thêm dạng câu.", ErrorType.Validation);
            db.BattleRooms.Add(room); db.BattleQuestions.AddRange(questions);
            db.BattleParticipants.Add(new() { BattleRoomId = room.Id, UserId = userId, LastConnectedAt = Now });
            await db.SaveChangesAsync(ct);
            return Result<BattleRoomDto>.Success(await View(room, userId, ct));
        }, ct);
    }

    public async Task<Result<BattleRoomDto>> FindAsync(Guid userId, string code, CancellationToken ct)
    {
        code = code.Trim().ToUpperInvariant();
        if (code.Length != 6) return Fail("Mã phòng gồm 6 ký tự.", ErrorType.Validation);
        var id = await db.BattleRooms.Where(x => x.JoinCode == code).Select(x => (Guid?)x.Id).SingleOrDefaultAsync(ct);
        return id is null ? Fail("Không tìm thấy phòng.", ErrorType.NotFound) : await GetAsync(userId, id.Value, ct);
    }

    public Task<Result<BattleRoomDto>> GetAsync(Guid userId, Guid roomId, CancellationToken ct) => Locked(roomId, async () =>
    {
        var room = await db.BattleRooms.SingleOrDefaultAsync(x => x.Id == roomId, ct);
        if (room is null) return Fail("Không tìm thấy phòng.", ErrorType.NotFound);
        if (await db.Relationships.AnyAsync(x => x.Kind == "Block" && ((x.UserId == userId && x.TargetUserId == room.HostUserId) || (x.UserId == room.HostUserId && x.TargetUserId == userId)), ct)) return Fail("Không tìm thấy phòng.", ErrorType.NotFound);
        var member = await db.BattleParticipants.SingleOrDefaultAsync(x => x.BattleRoomId == roomId && x.UserId == userId, ct);
        if (member?.Status == "Kicked" || (room.Status is not ("LobbyOpen" or "Locked") && member is null)) return Fail("Bạn không có quyền vào phòng này.", ErrorType.NotFound);
        await Advance(room, ct);
        return Result<BattleRoomDto>.Success(await View(room, userId, ct));
    }, ct);

    public async Task<Result<BattleRoomDto>> ActAsync(Guid userId, Guid roomId, string action, Guid? target, CancellationToken ct)
    {
        var result = await Locked(roomId, async () =>
        {
            var room = await db.BattleRooms.SingleOrDefaultAsync(x => x.Id == roomId, ct);
            if (room is null) return Fail("Không tìm thấy phòng.", ErrorType.NotFound);
            if (action == "join" && await db.Relationships.AnyAsync(x => x.Kind == "Block" && ((x.UserId == userId && x.TargetUserId == room.HostUserId) || (x.UserId == room.HostUserId && x.TargetUserId == userId)), ct)) return Fail("Không tìm thấy phòng.", ErrorType.NotFound);
            if (action is not ("join" or "leave") && room.HostUserId != userId) return Fail("Chỉ chủ phòng được thực hiện thao tác này.", ErrorType.NotFound);
            await Advance(room, ct);
            var member = await db.BattleParticipants.SingleOrDefaultAsync(x => x.BattleRoomId == roomId && x.UserId == userId, ct);
            if (action == "join")
            {
                if (member?.Status == "Kicked") return Fail("Bạn đã được chủ phòng đưa ra khỏi phòng.");
                if (member?.Status == "Active") { member.LastConnectedAt = Now; }
                else
                {
                    if (room.Status != "LobbyOpen") return Fail("Phòng đã khóa hoặc đã bắt đầu.");
                    if (await db.BattleParticipants.CountAsync(x => x.BattleRoomId == roomId && x.Status == "Active", ct) >= room.MaxPlayers) return Fail("Phòng đã đủ người.");
                    if (member is null) db.BattleParticipants.Add(new() { BattleRoomId = roomId, UserId = userId, LastConnectedAt = Now });
                    else { member.Status = "Active"; member.LastConnectedAt = Now; }
                }
            }
            else if (action == "leave")
            {
                if (room.HostUserId == userId) return Fail("Chủ phòng cần hủy trận trước khi rời phòng.");
                if (member is null || member.Status != "Active") return Fail("Bạn không ở trong phòng.");
                if (room.Status is "LobbyOpen" or "Locked") member.Status = "Left";
                // During play the participant stays enrolled so refresh/reconnect and timeout evidence remain consistent.
            }
            else if (action is "lock" or "unlock")
            {
                if (room.Status is not ("LobbyOpen" or "Locked")) return Fail("Chỉ có thể khóa phòng trong sảnh chờ.");
                room.Status = action == "lock" ? "Locked" : "LobbyOpen";
            }
            else if (action == "kick")
            {
                if (room.Status is not ("LobbyOpen" or "Locked") || target == userId) return Fail("Chỉ có thể loại người chơi khác trong sảnh chờ.");
                var player = await db.BattleParticipants.SingleOrDefaultAsync(x => x.BattleRoomId == roomId && x.UserId == target && x.Status == "Active", ct);
                if (player is null) return Fail("Không tìm thấy người chơi.", ErrorType.NotFound);
                player.Status = "Kicked";
            }
            else if (action == "start")
            {
                if (room.Status is not ("LobbyOpen" or "Locked")) return Fail("Trận đã bắt đầu hoặc kết thúc.");
                room.Status = "InProgress"; room.StartedAt = Now; room.CurrentQuestionIndex = 0; room.QuestionStartedAt = Now.AddSeconds(3);
            }
            else if (action == "next")
            {
                if (room.Status != "InProgress" || room.QuestionEndedAt is null) return Fail("Hãy chờ mọi người trả lời hoặc hết thời gian.");
                Next(room);
            }
            else if (action == "end-question")
            {
                if (room.Status != "InProgress" || room.QuestionEndedAt is not null || Now < room.QuestionStartedAt) return Fail("Câu hỏi chưa mở hoặc đã kết thúc.");
                await EndQuestion(room, ct);
            }
            else if (action == "cancel")
            {
                if (room.Status is "Completed" or "Expired" or "Cancelled") return Fail("Phòng đã kết thúc.");
                room.Status = "Cancelled"; room.CompletedAt = Now;
            }
            else return Fail("Thao tác không hợp lệ.", ErrorType.Validation);
            await db.SaveChangesAsync(ct);
            return Result<BattleRoomDto>.Success(await View(room, userId, ct));
        }, ct);
        if (result.IsSuccess) await notifier.ChangedAsync(roomId, ct);
        return result;
    }

    public async Task<Result<BattleRoomDto>> AnswerAsync(Guid userId, Guid roomId, Guid questionId, BattleAnswerRequest request, CancellationToken ct)
    {
        if (request.Answer is null || request.Answer.Length > 4000) return Fail("Đáp án tối đa 4.000 ký tự.", ErrorType.Validation);
        var result = await Locked(roomId, async () =>
        {
            var room = await db.BattleRooms.SingleOrDefaultAsync(x => x.Id == roomId, ct);
            var player = await db.BattleParticipants.SingleOrDefaultAsync(x => x.BattleRoomId == roomId && x.UserId == userId && x.Status == "Active", ct);
            if (room is null || player is null) return Fail("Không tìm thấy phòng hoặc người chơi.", ErrorType.NotFound);
            var q = await db.BattleQuestions.SingleOrDefaultAsync(x => x.Id == questionId && x.BattleRoomId == roomId, ct);
            if (q is null) return Fail("Không tìm thấy câu hỏi.", ErrorType.NotFound);
            var existing = await db.BattleAnswers.SingleOrDefaultAsync(x => x.BattleQuestionId == questionId && x.UserId == userId, ct);
            if (existing is not null) return existing.SubmittedAnswer == request.Answer.Trim() || existing.TimedOut
                ? Result<BattleRoomDto>.Success(await View(room, userId, ct)) : Fail("Bạn đã chốt đáp án cho câu này.");
            if (room.Status != "InProgress" || room.ExpiresAt <= Now || room.CurrentQuestionIndex != q.OrderIndex || room.QuestionEndedAt is not null || Now < room.QuestionStartedAt)
                return Fail("Câu hỏi chưa mở hoặc đã kết thúc.");
            if (q.TimeLimitSeconds is int seconds && Now >= room.QuestionStartedAt!.Value.AddSeconds(seconds))
            {
                await EndQuestion(room, ct);
                return Result<BattleRoomDto>.Success(await View(room, userId, ct));
            }
            if (q.QuestionType is "MultipleChoice" or "TrueFalse" && !BattleQuestionFactory.Options(q.OptionsJson).Any(x => x.Id == request.Answer.Trim()))
                return Fail("Hãy chọn một đáp án của câu hỏi này.", ErrorType.Validation);
            await Record(room, q, player, request.Answer.Trim(), false, ct);
            if (await db.BattleAnswers.CountAsync(x => x.BattleQuestionId == q.Id, ct) >= await db.BattleParticipants.CountAsync(x => x.BattleRoomId == roomId && x.Status == "Active", ct)) room.QuestionEndedAt = Now;
            await db.SaveChangesAsync(ct);
            return Result<BattleRoomDto>.Success(await View(room, userId, ct));
        }, ct);
        if (result.IsSuccess) await notifier.ChangedAsync(roomId, ct);
        return result;
    }

    private async Task Record(BattleRoom room, BattleQuestion q, BattleParticipant player, string submitted, bool timeout, CancellationToken ct)
    {
        var structured = q.QuestionType is "Matching" or "Ordering";
        if (structured && !timeout)
        {
            try { submitted = JsonSerializer.Serialize(JsonSerializer.Deserialize<string[]>(submitted)); }
            catch (JsonException) { submitted = "[]"; }
        }
        var attemptType = q.QuestionType == "MultipleChoice" ? LearningAttemptType.MultipleChoice : q.QuestionType == "TrueFalse" ? LearningAttemptType.TrueFalse : LearningAttemptType.Recall;
        var snapshot = Flashcard.Create(room.StudySetId, q.Prompt, q.CorrectAnswer, q.Explanation, 0,
            acceptedAnswers: q.QuestionType is "FillBlank" or "ShortAnswer" ? q.AcceptedAnswers : null);
        var evaluation = timeout ? LearningAttemptResult.Wrong : structured
            ? evaluator.EvaluateSequence(JsonSerializer.Deserialize<string[]>(q.CorrectAnswer)!, JsonSerializer.Deserialize<string[]>(submitted) ?? [])
            : evaluator.Evaluate(snapshot, RecallDirection.Forward, submitted, attemptType);
        var correct = evaluation == LearningAttemptResult.Correct;
        var duration = (int)Math.Clamp((Now - room.QuestionStartedAt!.Value).TotalMilliseconds, 0, int.MaxValue);
        if (q.TimeLimitSeconds is int limit) duration = Math.Min(duration, limit * 1000);
        player.Combo = correct ? player.Combo + 1 : 0;
        var score = correct ? 100 + (q.Difficulty == "Hard" ? 30 : q.Difficulty == "Medium" ? 15 : 0)
            + (player.Combo >= 10 ? 30 : player.Combo >= 5 ? 20 : player.Combo >= 3 ? 10 : 0)
            + (q.TimeLimitSeconds is int time ? (int)(20 * Math.Clamp(1d - duration / (time * 1000d), 0, 1)) : 0) : 0;
        var answer = new BattleAnswer { BattleRoomId = room.Id, BattleQuestionId = q.Id, UserId = player.UserId, SubmittedAnswer = submitted,
            Evaluation = evaluation.ToString(), IsCorrect = correct, ResponseTimeMs = duration, ScoreEarned = score, TimedOut = timeout, SubmittedAt = Now };
        // Material is copied into the learner's private review set only after submission/timeout.
        // Existing review APIs and the learning engine keep their ownership boundaries unchanged.
        if (player.ReviewStudySetId is null || !await db.StudySets.AnyAsync(s => s.Id == player.ReviewStudySetId, ct))
        {
            var subject = Subject.Create(player.UserId, "Live Battle", "Kiến thức đã luyện tập cùng bạn bè.");
            var reviewSet = StudySet.Create(subject.Id, room.Name, "Ôn lại các câu đã trả lời trong Live Battle.");
            db.Subjects.Add(subject); db.StudySets.Add(reviewSet); player.ReviewStudySetId = reviewSet.Id;
        }
        var reviewCard = Flashcard.Create(player.ReviewStudySetId.Value, q.Prompt, BattleQuestionFactory.DisplayAnswer(q), q.Explanation, q.OrderIndex,
            acceptedAnswers: q.QuestionType is "FillBlank" or "ShortAnswer" ? q.AcceptedAnswers : null);
        db.Flashcards.Add(reviewCard); answer.LearningFlashcardId = reviewCard.Id;
        player.Score += score; db.BattleAnswers.Add(answer); await db.SaveChangesAsync(ct);
        var evidence = await learning.RecordAsync(new RecordLearningEvidence(player.UserId, reviewCard.Id, null, StudyMode.Battle, attemptType,
            RecallDirection.Forward, submitted, evaluation, 0, duration, false, answer.Id,
            correct ? ReviewRating.Good : evaluation == LearningAttemptResult.Close ? ReviewRating.Hard : ReviewRating.Again,
            correct ? LearningCardOutcome.Completed : LearningCardOutcome.NeedsReview), ct);
        if (!evidence.IsSuccess) throw new InvalidOperationException("Battle learning evidence could not be recorded: " + evidence.Error!.Code);
    }

    private async Task EndQuestion(BattleRoom room, CancellationToken ct)
    {
        var q = await db.BattleQuestions.SingleAsync(x => x.BattleRoomId == room.Id && x.OrderIndex == room.CurrentQuestionIndex, ct);
        var missing = await db.BattleParticipants.Where(x => x.BattleRoomId == room.Id && x.Status == "Active" && !db.BattleAnswers.Any(a => a.BattleQuestionId == q.Id && a.UserId == x.UserId)).OrderBy(x => x.UserId).ToListAsync(ct);
        foreach (var player in missing) await Record(room, q, player, "", true, ct);
        room.QuestionEndedAt = Now;
        await db.SaveChangesAsync(ct);
    }

    private void Next(BattleRoom room)
    {
        if (room.CurrentQuestionIndex + 1 >= room.QuestionCount) { room.Status = "Completed"; room.CompletedAt = Now; }
        else { room.CurrentQuestionIndex++; room.QuestionStartedAt = Now.AddSeconds(2); room.QuestionEndedAt = null; }
    }

    private async Task Advance(BattleRoom room, CancellationToken ct)
    {
        if (room.Status is "Completed" or "Cancelled" or "Expired") return;
        if (room.ExpiresAt <= Now) { room.Status = "Expired"; room.CompletedAt = Now; }
        else if (room.Status == "InProgress")
        {
            if (room.QuestionEndedAt is not null && Now >= room.QuestionEndedAt.Value.AddSeconds(8)) Next(room);
            else if (room.QuestionEndedAt is null)
            {
                var q = await db.BattleQuestions.SingleAsync(x => x.BattleRoomId == room.Id && x.OrderIndex == room.CurrentQuestionIndex, ct);
                if (q.TimeLimitSeconds is int seconds && Now >= room.QuestionStartedAt!.Value.AddSeconds(seconds)) await EndQuestion(room, ct);
            }
        }
        await db.SaveChangesAsync(ct);
    }

    public async Task TickAsync(CancellationToken ct)
    {
        var ids = await db.BattleRooms.AsNoTracking().Where(x => x.Status == "InProgress" || ((x.Status == "LobbyOpen" || x.Status == "Locked") && x.ExpiresAt <= Now)).Select(x => x.Id).ToListAsync(ct);
        foreach (var id in ids)
        {
            var changed = false;
            await Locked(id, async () =>
            {
                db.ChangeTracker.Clear();
                var room = await db.BattleRooms.SingleAsync(x => x.Id == id, ct);
                var before = (room.Status, room.CurrentQuestionIndex, room.QuestionEndedAt);
                await Advance(room, ct);
                changed = before != (room.Status, room.CurrentQuestionIndex, room.QuestionEndedAt);
                return Result<BattleRoomDto>.Success(null!);
            }, ct);
            if (changed) await notifier.ChangedAsync(id, ct);
        }
    }

    private async Task<BattleRoomDto> View(BattleRoom room, Guid userId, CancellationToken ct)
    {
        var participants = await db.BattleParticipants.Where(x => x.BattleRoomId == room.Id && x.Status == "Active").ToListAsync(ct);
        var member = participants.SingleOrDefault(x => x.UserId == userId);
        var participantIds = participants.Select(p => p.UserId).ToList();
        var names = await db.Users.Where(x => participantIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.DisplayName, ct);
        var questions = await db.BattleQuestions.Where(x => x.BattleRoomId == room.Id).OrderBy(x => x.OrderIndex).ToListAsync(ct);
        var answers = await db.BattleAnswers.Where(x => x.BattleRoomId == room.Id).ToListAsync(ct);
        var revealRanks = room.Status == "Completed" || room.LeaderboardMode == "Always" || room.LeaderboardMode == "BetweenRounds" && room.QuestionEndedAt is not null;
        var ranked = participants.OrderByDescending(p => p.Score)
            .ThenByDescending(p => answers.Count(a => a.UserId == p.UserId && a.IsCorrect))
            .ThenByDescending(p => answers.Count(a => a.UserId == p.UserId && a.IsCorrect && questions.Any(q => q.Id == a.BattleQuestionId && q.Difficulty == "Hard")))
            .ThenBy(p => answers.Where(a => a.UserId == p.UserId).Select(a => a.ResponseTimeMs).DefaultIfEmpty().Average()).ThenBy(p => p.CreatedAt).ToList();
        var players = (revealRanks ? ranked.AsEnumerable() : participants.OrderBy(p => p.CreatedAt)).Select(p =>
        {
            var own = answers.Where(a => a.UserId == p.UserId).ToArray();
            return new BattlePlayerDto(p.UserId, names[p.UserId], p.UserId == room.HostUserId, p.Status, revealRanks ? p.Score : null,
                revealRanks ? ranked.IndexOf(p) + 1 : null, revealRanks ? own.Count(a => a.IsCorrect) : null,
                revealRanks ? own.Length == 0 ? 0 : Math.Round(100d * own.Count(a => a.IsCorrect) / own.Length) : null,
                revealRanks ? Math.Round(own.Select(a => a.ResponseTimeMs / 1000d).DefaultIfEmpty().Average(), 1) : null);
        }).ToArray();
        var current = member is null || room.Status != "InProgress" ? null : questions.Single(x => x.OrderIndex == room.CurrentQuestionIndex);
        var mine = current is null ? null : answers.SingleOrDefault(x => x.BattleQuestionId == current.Id && x.UserId == userId);
        var reviews = member is not null && room.Status == "Completed" ? questions.Select(q => new BattleReview(q.Id, q.Prompt, BattleQuestionFactory.DisplayAnswer(q), q.Explanation,
            answers.SingleOrDefault(a => a.BattleQuestionId == q.Id && a.UserId == userId)?.Evaluation ?? "Wrong", userId == room.HostUserId ? Source(q) : null,
            participants.Count == 0 ? 0 : Math.Round(100d * answers.Count(a => a.BattleQuestionId == q.Id && a.IsCorrect) / participants.Count))).ToArray() : [];
        return new(room.Id, room.StudySetId, room.HostUserId, room.Name, room.JoinCode, room.Status, room.Mode, room.Difficulty, room.LeaderboardMode,
            room.MaxPlayers, room.QuestionCount, room.DefaultTimeLimitSeconds, Now, participants.Count, member is not null,
            current is null ? 0 : answers.Count(x => x.BattleQuestionId == current.Id), member?.Score ?? 0, member?.Combo ?? 0,
            member is null ? [] : players,
            current is null ? null : new(current.Id, current.QuestionType, current.Prompt, current.Difficulty, current.OrderIndex + 1, current.TimeLimitSeconds,
                room.QuestionStartedAt!.Value, room.QuestionEndedAt, BattleQuestionFactory.Options(current.OptionsJson), BattleQuestionFactory.Options(current.LeftItemsJson)),
            mine is null ? null : new(current!.Id, mine.Evaluation, mine.ScoreEarned, BattleQuestionFactory.DisplayAnswer(current), current.Explanation, userId == room.HostUserId ? Source(current) : null), reviews,
            room.Status == "Completed" ? member?.ReviewStudySetId : null);
    }
    private static object? Source(BattleQuestion q) => q.SourceJson is null ? null : JsonSerializer.Deserialize<JsonElement>(q.SourceJson);
}
