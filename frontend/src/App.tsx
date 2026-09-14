import { Navigate, Route, Routes } from "react-router-dom";
import { AdminCohortsPage } from "./features/admin/AdminCohortsPage";
import { AdminGate } from "./features/admin/AdminGate";
import { AdminModulesPage } from "./features/admin/AdminModulesPage";
import { AdminStudentProgressPage } from "./features/admin/AdminStudentProgressPage";
import { AdminStudentsPage } from "./features/admin/AdminStudentsPage";
import { AuthGate } from "./auth/AuthGate";
import { DashboardPage } from "./features/dashboard/DashboardPage";
import { LoginPage } from "./features/login/LoginPage";
import { ModulePage } from "./features/module/ModulePage";
import { LevelPreviewPage } from "./features/level/LevelPreviewPage";
import { PracticePage } from "./features/practice/PracticePage";
import { IntroPage } from "./features/intro/IntroPage";
import { SourcePage } from "./features/source/SourcePage";
import { TestPage } from "./features/test/TestPage";

export function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route
        path="/"
        element={
          <AuthGate>
            <DashboardPage />
          </AuthGate>
        }
      />
      <Route
        path="/modules/:moduleSlug"
        element={
          <AuthGate>
            <ModulePage />
          </AuthGate>
        }
      />
      <Route
        path="/modules/:moduleSlug/sources/:sourceSlug"
        element={
          <AuthGate>
            <SourcePage />
          </AuthGate>
        }
      />
      <Route
        path="/modules/:moduleSlug/sources/:sourceSlug/intro"
        element={
          <AuthGate>
            <IntroPage />
          </AuthGate>
        }
      />
      <Route
        path="/modules/:moduleSlug/sources/:sourceSlug/practice"
        element={
          <AuthGate>
            <PracticePage />
          </AuthGate>
        }
      />
      <Route
        path="/modules/:moduleSlug/sources/:sourceSlug/levels/:levelId"
        element={
          <AuthGate>
            <LevelPreviewPage />
          </AuthGate>
        }
      />
      <Route
        path="/modules/:moduleSlug/sources/:sourceSlug/test/:levelId"
        element={
          <AuthGate>
            <TestPage />
          </AuthGate>
        }
      />
      <Route
        path="/sessions/:sessionId"
        element={
          <AuthGate>
            <TestPage />
          </AuthGate>
        }
      />
      <Route
        path="/admin"
        element={
          <AuthGate>
            <AdminGate>
              <Navigate to="/admin/modules" replace />
            </AdminGate>
          </AuthGate>
        }
      />
      <Route
        path="/admin/modules"
        element={
          <AuthGate>
            <AdminGate>
              <AdminModulesPage />
            </AdminGate>
          </AuthGate>
        }
      />
      <Route
        path="/admin/cohorts"
        element={
          <AuthGate>
            <AdminGate>
              <AdminCohortsPage />
            </AdminGate>
          </AuthGate>
        }
      />
      <Route
        path="/admin/students"
        element={
          <AuthGate>
            <AdminGate>
              <AdminStudentsPage />
            </AdminGate>
          </AuthGate>
        }
      />
      <Route
        path="/admin/students/:userId"
        element={
          <AuthGate>
            <AdminGate>
              <AdminStudentProgressPage />
            </AdminGate>
          </AuthGate>
        }
      />
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}
