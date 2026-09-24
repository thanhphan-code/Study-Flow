import { useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Link, useNavigate, useParams } from "react-router-dom";
import { StudySetForm } from "@/features/studySets/components/StudySetForm";
import {
  studySetKeys,
  studySetsApi,
} from "@/features/studySets/api/studySetsApi";
import { FlashcardEditor, type FlashcardCreateDraft } from "@/features/flashcards/components/FlashcardEditor";
import { FlashcardListItem } from "@/features/flashcards/components/FlashcardListItem";
import {
  flashcardKeys,
  flashcardsApi,
} from "@/features/flashcards/api/flashcardsApi";
import type { FlashcardInput } from "@/features/flashcards/types/flashcard";
import { QuizPanel } from "@/features/quizzes/components/QuizPanel";
import { StudySetProgressPanel } from "@/features/insights/components/StudySetProgressPanel";
import { DocumentUploader } from "@/features/documents/components/DocumentUploader";
import { AIGenerator } from "@/features/ai/components/AIGenerator";
import { useLanguage } from "@/i18n/LanguageProvider";
import { BookOpenCheck, Play, Settings2, Swords, Share2 } from "lucide-react";

export function StudySetDetailPage() {
  const { id = "" } = useParams();
  const [editing, setEditing] = useState(false);
  const [addingCards, setAddingCards] = useState(false);
  const [imageUploadNotice, setImageUploadNotice] = useState("");
  const [cardSearch, setCardSearch] = useState("");
  const [visibleCardCount, setVisibleCardCount] = useState(20);
  const queryClient = useQueryClient();
  const navigate = useNavigate();
  const { language, t } = useLanguage();
  const vi = language === "vi";
  const copy = vi ? { loading: "Đang tải bộ học…", notFound: "Không tìm thấy bộ học", returnSubjects: "Về danh sách môn học", backSubject: "Về môn học", editSet: "Sửa bộ học", set: "Bộ học", viewDocs: "Xem tài liệu đã nhập", flashcards: "Flashcard", material: "Tạo nội dung rồi luyện khả năng chủ động nhớ lại.", study: "Học bằng thẻ", smartLearn: "Bắt đầu học", manage: "Quản lý nội dung", loadingCards: "Đang tải flashcard…", loadError: "Không thể tải flashcard.", empty: "Chưa có flashcard", emptyHelp: "Thêm câu hỏi và đáp án đầu tiên để bắt đầu học.", confirmCard: "Xóa flashcard này?", confirmSet: "Xóa bộ học và toàn bộ flashcard bên trong?", deleting: "Đang xóa…", deleteSet: "Xóa bộ học" } : { loading: "Loading study set…", notFound: "Study set not found", returnSubjects: "Return to subjects", backSubject: "Back to subject", editSet: "Edit study set", set: "Study set", viewDocs: "View imported documents", flashcards: "Flashcards", material: "Build the material, then practice active recall.", study: "Study cards", smartLearn: "Start learning", manage: "Manage content", loadingCards: "Loading flashcards…", loadError: "Unable to load flashcards.", empty: "No flashcards yet", emptyHelp: "Add your first question and answer to start learning.", confirmCard: "Delete this flashcard?", confirmSet: "Delete this study set and all of its flashcards?", deleting: "Deleting…", deleteSet: "Delete study set" };
  const studySet = useQuery({
    queryKey: studySetKeys.detail(id),
    queryFn: () => studySetsApi.get(id),
    enabled: Boolean(id),
  });
  const cards = useQuery({
    queryKey: flashcardKeys.byStudySet(id),
    queryFn: () => flashcardsApi.list(id),
    enabled: Boolean(id),
  });
  const update = useMutation({
    mutationFn: (input: { title: string; description: string | null }) =>
      studySetsApi.update(id, input),
    onSuccess: async (value) => {
      queryClient.setQueryData(studySetKeys.detail(id), value);
      await queryClient.invalidateQueries({
        queryKey: studySetKeys.bySubject(value.subjectId),
      });
      setEditing(false);
    },
  });
  const remove = useMutation({
    mutationFn: () => studySetsApi.remove(id),
    onSuccess: async () => {
      if (studySet.data)
        await queryClient.invalidateQueries({
          queryKey: studySetKeys.bySubject(studySet.data.subjectId),
        });
      navigate(`/subjects/${studySet.data?.subjectId}`, { replace: true });
    },
  });
  const addCards = useMutation({
    mutationFn: async (drafts: FlashcardCreateDraft[]) => {
      const created = drafts.length === 1
        ? [await flashcardsApi.create(id, drafts[0].input)]
        : await flashcardsApi.bulkCreate(id, drafts.map((draft) => draft.input));
      const uploads = await Promise.allSettled(drafts.map((draft, index) => draft.imageFile ? flashcardsApi.uploadImage(created[index].id, draft.imageFile) : Promise.resolve(created[index])));
      return { failedImages: uploads.filter((result) => result.status === "rejected").length };
    },
    onSuccess: async ({ failedImages }) => {
      await queryClient.invalidateQueries({
        queryKey: flashcardKeys.byStudySet(id),
      });
      setImageUploadNotice(failedImages ? (vi ? `${failedImages} ảnh không tải lên được. Thẻ văn bản vẫn đã được lưu; bạn có thể thêm lại ảnh trong danh sách bên dưới.` : `${failedImages} image(s) could not be uploaded. The cards were saved; you can add the images from the list below.`) : "");
      setAddingCards(false);
    },
  });
  const updateCard = useMutation({
    mutationFn: ({
      cardId,
      input,
    }: {
      cardId: string;
      input: FlashcardInput;
    }) => flashcardsApi.update(cardId, input),
    onSuccess: () =>
      queryClient.invalidateQueries({ queryKey: flashcardKeys.byStudySet(id) }),
  });
  const deleteCard = useMutation({
    mutationFn: flashcardsApi.remove,
    onSuccess: () =>
      queryClient.invalidateQueries({ queryKey: flashcardKeys.byStudySet(id) }),
  });
  const uploadCardImage = useMutation({ mutationFn: ({ cardId, file }: { cardId: string; file: File }) => flashcardsApi.uploadImage(cardId, file), onSuccess: () => queryClient.invalidateQueries({ queryKey: flashcardKeys.byStudySet(id) }) });
  const deleteCardImage = useMutation({ mutationFn: flashcardsApi.removeImage, onSuccess: () => queryClient.invalidateQueries({ queryKey: flashcardKeys.byStudySet(id) }) });
  const filteredCards = useMemo(() => {
    const query = cardSearch.trim().toLocaleLowerCase();
    if (!query) return cards.data ?? [];
    return (cards.data ?? []).filter((card) => [card.frontText, card.backText, card.explanation, card.readingText, card.romanization].some((text) => text?.toLocaleLowerCase().includes(query)));
  }, [cardSearch, cards.data]);
  const visibleCards = filteredCards.slice(0, visibleCardCount);
  if (studySet.isPending)
    return (
      <main className="grid min-h-screen place-items-center text-slate-500">
        {copy.loading}
      </main>
    );
  if (studySet.isError || !studySet.data)
    return (
      <main className="grid min-h-screen place-items-center">
        <div className="text-center">
          <h1 className="text-2xl font-semibold">{copy.notFound}</h1>
          <Link to="/subjects" className="mt-4 inline-block text-indigo-600">
            {copy.returnSubjects}
          </Link>
        </div>
      </main>
    );
  const value = studySet.data;
  return (
    <main className="mx-auto min-h-screen max-w-5xl px-6 py-12">
      <Link
        to={`/subjects/${value.subjectId}`}
        className="text-sm font-semibold text-indigo-600"
      >
        ← {copy.backSubject}
      </Link>
      <section className="mt-8 rounded-3xl border border-slate-200 bg-white p-8">
        {editing ? (
          <>
            <h1 className="mb-6 text-2xl font-semibold">{copy.editSet}</h1>
            <StudySetForm
              initial={{ title: value.title, description: value.description }}
              pending={update.isPending}
              onCancel={() => setEditing(false)}
              onSubmit={(input) =>
                update.mutateAsync(input).then(() => undefined)
              }
            />
          </>
        ) : (
          <>
            <div className="flex flex-wrap items-start justify-between gap-5">
              <div>
                <p className="text-sm font-semibold uppercase tracking-[0.16em] text-indigo-600">
                  {copy.set}
                </p>
                <h1 className="mt-3 text-4xl font-semibold tracking-tight text-slate-950">
                  {value.title}
                </h1>
                <p className="mt-4 leading-7 text-slate-600">
                  {value.description ?? t("noDescription")}
                </p>
              </div>
            </div>
          </>
        )}
      </section>
      {value.type === "Combined" && <section className="mt-6 rounded-2xl border border-indigo-200 bg-indigo-50 p-5"><p className="text-sm font-bold text-indigo-700">{vi ? "Được kết hợp từ" : "Combined from"}</p><div className="mt-3 flex flex-wrap gap-2">{value.sources.map(source => <Link key={source.id} to={`/study-sets/${source.id}`} className="rounded-full bg-white px-3 py-2 text-sm font-semibold text-indigo-700">{source.title}</Link>)}</div><Link to={`/subjects/${value.subjectId}/combine`} state={{reviewSetId:value.id,title:value.title,sourceIds:value.sources.map(source=>source.id)}} className="mt-4 inline-block text-sm font-bold text-indigo-700">✎ {vi ? "Sửa các bộ nguồn" : "Edit sources"}</Link></section>}
      {value.type === "Standard" && <details className="sf-card group mt-6 rounded-2xl p-5">
        <summary className="flex cursor-pointer list-none items-center justify-between gap-4 font-semibold text-slate-800">
          <span><span className="mr-2 text-blue-600">＋</span>{vi ? "Thêm nội dung từ tài liệu hoặc AI" : "Add material from documents or AI"}</span>
          <span className="text-sm font-medium text-slate-400 transition-transform group-open:rotate-180">⌄</span>
        </summary>
        <div className="mt-5 border-t border-slate-100 pt-5"><AIGenerator studySetId={id} /><div className="mt-5"><DocumentUploader studySetId={id} /><Link to="/documents" className="mt-3 inline-block text-sm font-semibold text-indigo-600">{copy.viewDocs} →</Link></div></div>
      </details>}
      <StudySetProgressPanel studySetId={id} />
      <section className="mt-10">
        <div className="flex flex-wrap items-end justify-between gap-4">
          <div>
            <h2 className="text-2xl font-semibold text-slate-950">
              {copy.flashcards}{" "}
              <span className="text-slate-400">{cards.data?.length ?? 0}</span>
            </h2>
            <p className="mt-1 text-sm text-slate-500">
              {copy.material}
            </p>
          </div>
          <div className="flex flex-wrap items-center justify-end gap-3">
            <Link to={`/study-sets/${id}/battle`} className="inline-flex min-h-12 items-center gap-2 rounded-xl border border-blue-200 bg-blue-50 px-5 py-3 font-semibold text-blue-700"><Swords aria-hidden size={18}/>Live Battle</Link>
            <Link to={`/community/sets/${id}`} className="inline-flex min-h-12 items-center gap-2 rounded-xl border border-blue-200 bg-white px-5 py-3 font-semibold text-blue-700"><Share2 aria-hidden size={18}/>{vi ? 'Chia sẻ' : 'Share'}</Link>
            {!!cards.data?.length && <Link to={`/study/${id}/learn`} className="sf-primary inline-flex min-h-12 items-center gap-2 rounded-xl px-5 py-3 font-semibold text-white"><Play aria-hidden size={18} fill="currentColor"/>{copy.smartLearn}</Link>}
            {!!cards.data?.length && <Link to={`/study/${id}/flashcards`} className="inline-flex min-h-12 items-center gap-2 rounded-xl border border-indigo-200 bg-white px-5 py-3 font-semibold text-indigo-700"><BookOpenCheck aria-hidden size={18}/>{copy.study}</Link>}
            {value.type === "Standard" && <details className="relative"><summary className="inline-flex min-h-12 cursor-pointer list-none items-center gap-2 rounded-xl border border-slate-200 bg-white px-4 py-3 font-semibold text-slate-600"><Settings2 aria-hidden size={18}/>{copy.manage}</summary><div className="absolute right-0 z-20 mt-2 grid min-w-56 gap-1 rounded-2xl border border-slate-200 bg-white p-2 shadow-lg"><button onClick={()=>setAddingCards(current=>!current)} className="rounded-xl px-3 py-2 text-left text-sm font-semibold text-slate-700 hover:bg-slate-50">{t('addCards')}</button><Link to={`/subjects/${value.subjectId}/combine`} className="rounded-xl px-3 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-50">{vi?'Tạo bộ ôn tập':'Create review set'}</Link><button onClick={()=>setEditing(true)} className="rounded-xl px-3 py-2 text-left text-sm font-semibold text-slate-700 hover:bg-slate-50">{copy.editSet}</button></div></details>}
          </div>
        </div>
        {(cards.data?.length ?? 0) > 8 && <div className="mt-5 flex items-center gap-3 rounded-2xl border border-slate-200 bg-white/80 px-4 py-3 shadow-sm">
          <span aria-hidden className="text-slate-400">⌕</span>
          <input value={cardSearch} onChange={(event) => { setCardSearch(event.target.value); setVisibleCardCount(20) }} placeholder={vi ? "Tìm câu hỏi, đáp án hoặc cách đọc…" : "Search questions, answers, or readings…"} className="min-w-0 flex-1 bg-transparent text-sm text-slate-800 outline-none placeholder:text-slate-400" />
          {cardSearch && <button type="button" onClick={() => setCardSearch("")} className="text-xs font-semibold text-slate-500">{vi ? "Xóa lọc" : "Clear"}</button>}
        </div>}
        {addingCards && (
          <div className="mt-6 rounded-2xl border border-slate-200 bg-white p-6">
            <FlashcardEditor
              pending={addCards.isPending}
              onCancel={() => setAddingCards(false)}
              onSave={(values) =>
                addCards.mutateAsync(values).then(() => undefined)
              }
            />
          </div>
        )}
        {imageUploadNotice && <p role="alert" className="mt-4 rounded-xl bg-amber-50 p-4 text-sm text-amber-800">{imageUploadNotice}</p>}
        {cards.isPending && (
          <p className="mt-6 text-slate-500">{copy.loadingCards}</p>
        )}
        {cards.isError && (
          <p className="mt-6 rounded-xl bg-red-50 p-4 text-red-700">
            {copy.loadError}
          </p>
        )}
        {cards.data?.length === 0 && !addingCards && (
          <div className="mt-6 rounded-2xl border border-dashed border-slate-300 p-8 text-center"><h3 className="font-semibold text-slate-900">{copy.empty}</h3><p className="mt-2 text-sm text-slate-500">{copy.emptyHelp}</p>{value.type==='Standard'&&<button type="button" onClick={()=>setAddingCards(true)} className="sf-primary mt-5 min-h-11 rounded-xl px-5 py-2.5 font-semibold text-white">{t('addCards')}</button>}</div>
        )}
        <div className="mt-6 space-y-3">
          {value.type === "Combined" ? visibleCards.map(card => <article key={card.id} className="rounded-xl border border-slate-200 bg-white p-4"><p className="text-xs font-bold text-indigo-500">{value.sources.find(source => source.id === card.studySetId)?.title}</p><div className="mt-2 grid gap-2 sm:grid-cols-2"><strong>{card.frontText}</strong><span className="text-slate-600">{card.backText}</span></div></article>) : visibleCards.map((card) => (
            <FlashcardListItem
              key={card.id}
              card={card}
              pending={updateCard.isPending}
              onUpdate={(input) =>
                updateCard
                  .mutateAsync({ cardId: card.id, input })
                  .then(() => undefined)
              }
              onDelete={() =>
                window.confirm(copy.confirmCard) &&
                deleteCard.mutate(card.id)
              }
              onUploadImage={(file) => uploadCardImage.mutateAsync({ cardId: card.id, file }).then(() => undefined)}
              onDeleteImage={() => deleteCardImage.mutateAsync(card.id)}
            />
          ))}
        </div>
        {filteredCards.length === 0 && cardSearch && <p className="mt-6 rounded-2xl border border-dashed border-slate-300 p-8 text-center text-sm text-slate-500">{vi ? "Không tìm thấy flashcard phù hợp." : "No matching flashcards found."}</p>}
        {visibleCardCount < filteredCards.length && <button type="button" onClick={() => setVisibleCardCount((count) => count + 20)} className="mt-5 w-full rounded-xl border border-slate-200 bg-white py-3 text-sm font-semibold text-indigo-700 hover:border-indigo-300 hover:bg-indigo-50">{vi ? `Hiển thị thêm (${filteredCards.length - visibleCardCount})` : `Show more (${filteredCards.length - visibleCardCount})`}</button>}
      </section>
      <QuizPanel studySetId={id} cardCount={cards.data?.length ?? 0} />
      <section className="mt-12 border-t border-slate-200 pt-8">
        <button
          disabled={remove.isPending}
          onClick={() =>
            window.confirm(
              copy.confirmSet,
            ) && remove.mutate()
          }
          className="text-sm font-semibold text-red-600 hover:underline"
        >
          {remove.isPending ? copy.deleting : copy.deleteSet}
        </button>
      </section>
    </main>
  );
}
