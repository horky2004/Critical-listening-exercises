import { Navigate, Route, Routes } from "react-router-dom";
import { AuthGate } from "./auth/AuthGate";
import { DashboardPage } from "./features/dashboard/DashboardPage";
import { LoginPage } from "./features/login/LoginPage";
import { ModulePage } from "./features/module/ModulePage";
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
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}
