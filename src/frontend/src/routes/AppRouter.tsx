import { lazy, Suspense } from "react";
import { Navigate, Route, Routes } from "react-router-dom";
import { HomePage } from "@/pages/HomePage";
import { NotFoundPage } from "@/pages/NotFoundPage";
import { AuthPage } from "@/features/auth/pages/AuthPage";
import { ProtectedRoute } from "@/features/auth/components/ProtectedRoute";
import { AdminRoute } from "@/features/admin/AdminRoute";
import { SubjectsPage } from "@/features/subjects/pages/SubjectsPage";
import { SubjectDetailPage } from "@/features/subjects/pages/SubjectDetailPage";
import { StudySetDetailPage } from "@/features/studySets/pages/StudySetDetailPage";
import { FlashcardStudyPage } from "@/features/flashcards/pages/FlashcardStudyPage";
import { DueReviewPage } from "@/features/progress/pages/DueReviewPage";
import { QuizPage } from "@/features/quizzes/pages/QuizPage";
import { ProgressPage } from "@/features/insights/pages/ProgressPage";
import { DocumentsPage } from "@/features/documents/pages/DocumentsPage";
import { DocumentDetailPage } from "@/features/documents/pages/DocumentDetailPage";
import { SmartLearnPage } from "@/features/smartLearning/pages/SmartLearnPage";
import { DailyStudyPage } from "@/features/dailyStudy/pages/DailyStudyPage";
import { CombineStudySetsPage } from "@/features/studySets/pages/CombineStudySetsPage";
import { StudyTogetherPage } from "@/features/studySets/pages/StudyTogetherPage";
const CreateBattlePage = lazy(() =>
  import("@/features/battles/CreateBattlePage").then((module) => ({
    default: module.CreateBattlePage,
  })),
);
const JoinBattlePage = lazy(() =>
  import("@/features/battles/JoinBattlePage").then((module) => ({
    default: module.JoinBattlePage,
  })),
);
const BattleRoomPage = lazy(() =>
  import("@/features/battles/BattleRoomPage").then((module) => ({
    default: module.BattleRoomPage,
  })),
);
const ExplorePage = lazy(() =>
  import("@/features/social/ExplorePage").then((m) => ({
    default: m.ExplorePage,
  })),
);
const ProfilePage = lazy(() =>
  import("@/features/social/ProfilePage").then((m) => ({
    default: m.ProfilePage,
  })),
);
const FriendsPage = lazy(() =>
  import("@/features/social/ProfilePage").then((m) => ({
    default: m.FriendsPage,
  })),
);
const CommunitySetPage = lazy(() =>
  import("@/features/social/CommunitySetPage").then((m) => ({
    default: m.CommunitySetPage,
  })),
);
const MessagesPage = lazy(() =>
  import("@/features/social/MessagesPage").then((m) => ({
    default: m.MessagesPage,
  })),
);
const NotificationsPage = lazy(() =>
  import("@/features/social/MessagesPage").then((m) => ({
    default: m.NotificationsPage,
  })),
);
const ModerationPage = lazy(() =>
  import("@/features/social/ModerationPage").then((m) => ({
    default: m.ModerationPage,
  })),
);
const AdminPage = lazy(() =>
  import("@/features/admin/AdminPage").then((m) => ({ default: m.AdminPage })),
);

export function AppRouter() {
  return (
    <Routes>
      <Route
        path="/admin"
        element={
          <AdminRoute>
            <Suspense
              fallback={
                <p role="status" className="p-8">
                  Đang tải bảng quản trị…
                </p>
              }
            >
              <AdminPage />
            </Suspense>
          </AdminRoute>
        }
      />
      <Route
        path="/community/moderation"
        element={
          <ProtectedRoute>
            <Suspense fallback={<p role="status">Đang tải…</p>}>
              <ModerationPage />
            </Suspense>
          </ProtectedRoute>
        }
      />
      <Route
        path="/explore"
        element={
          <ProtectedRoute>
            <Suspense fallback={<p role="status">Đang tải…</p>}>
              <ExplorePage />
            </Suspense>
          </ProtectedRoute>
        }
      />
      <Route
        path="/community/saved"
        element={
          <ProtectedRoute>
            <Suspense fallback={<p role="status">Đang tải…</p>}>
              <ExplorePage saved />
            </Suspense>
          </ProtectedRoute>
        }
      />
      <Route
        path="/people/:username"
        element={
          <ProtectedRoute>
            <Suspense fallback={<p role="status">Đang tải…</p>}>
              <ProfilePage />
            </Suspense>
          </ProtectedRoute>
        }
      />
      <Route
        path="/community/friends"
        element={
          <ProtectedRoute>
            <Suspense fallback={<p role="status">Đang tải…</p>}>
              <FriendsPage />
            </Suspense>
          </ProtectedRoute>
        }
      />
      <Route
        path="/community/sets/:id"
        element={
          <ProtectedRoute>
            <Suspense fallback={<p role="status">Đang tải…</p>}>
              <CommunitySetPage />
            </Suspense>
          </ProtectedRoute>
        }
      />
      <Route
        path="/messages"
        element={
          <ProtectedRoute>
            <Suspense fallback={<p role="status">Đang tải…</p>}>
              <MessagesPage />
            </Suspense>
          </ProtectedRoute>
        }
      />
      <Route
        path="/messages/:id"
        element={
          <ProtectedRoute>
            <Suspense fallback={<p role="status">Đang tải…</p>}>
              <MessagesPage />
            </Suspense>
          </ProtectedRoute>
        }
      />
      <Route
        path="/notifications"
        element={
          <ProtectedRoute>
            <Suspense fallback={<p role="status">Đang tải…</p>}>
              <NotificationsPage />
            </Suspense>
          </ProtectedRoute>
        }
      />
      <Route
        path="/study-sets/:id/battle"
        element={
          <ProtectedRoute>
            <Suspense
              fallback={
                <p role="status" className="p-8">
                  Đang tải Battle…
                </p>
              }
            >
              <CreateBattlePage />
            </Suspense>
          </ProtectedRoute>
        }
      />
      <Route
        path="/join"
        element={
          <ProtectedRoute>
            <Suspense
              fallback={
                <p role="status" className="p-8">
                  Đang tải Battle…
                </p>
              }
            >
              <JoinBattlePage />
            </Suspense>
          </ProtectedRoute>
        }
      />
      <Route
        path="/join/:code"
        element={
          <ProtectedRoute>
            <Suspense
              fallback={
                <p role="status" className="p-8">
                  Đang tải Battle…
                </p>
              }
            >
              <JoinBattlePage />
            </Suspense>
          </ProtectedRoute>
        }
      />
      <Route
        path="/battles/:id"
        element={
          <ProtectedRoute>
            <Suspense
              fallback={
                <p role="status" className="p-8">
                  Đang tải Battle…
                </p>
              }
            >
              <BattleRoomPage />
            </Suspense>
          </ProtectedRoute>
        }
      />
      <Route path="/" element={<Navigate to="/home" replace />} />
      <Route path="/login" element={<AuthPage mode="login" />} />
      <Route path="/register" element={<AuthPage mode="register" />} />
      <Route
        path="/home"
        element={
          <ProtectedRoute>
            <HomePage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/subjects"
        element={
          <ProtectedRoute>
            <SubjectsPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/subjects/:id"
        element={
          <ProtectedRoute>
            <SubjectDetailPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/study-sets/:id"
        element={
          <ProtectedRoute>
            <StudySetDetailPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/study/:id/flashcards"
        element={
          <ProtectedRoute>
            <FlashcardStudyPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/study/:id/learn"
        element={
          <ProtectedRoute>
            <SmartLearnPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/study/today"
        element={
          <ProtectedRoute>
            <DailyStudyPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/subjects/:id/combine"
        element={
          <ProtectedRoute>
            <CombineStudySetsPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/study-together"
        element={
          <ProtectedRoute>
            <StudyTogetherPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/reviews/due"
        element={
          <ProtectedRoute>
            <DueReviewPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/documents"
        element={
          <ProtectedRoute>
            <DocumentsPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/documents/:id"
        element={
          <ProtectedRoute>
            <DocumentDetailPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/progress"
        element={
          <ProtectedRoute>
            <ProgressPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/quizzes/:id"
        element={
          <ProtectedRoute>
            <QuizPage />
          </ProtectedRoute>
        }
      />
      <Route path="*" element={<NotFoundPage />} />
    </Routes>
  );
}
