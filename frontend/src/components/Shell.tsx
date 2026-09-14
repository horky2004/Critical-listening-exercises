import type { ReactNode } from "react";
import { Link, useLocation, useNavigate } from "react-router-dom";
import { useMe } from "../api/hooks";
import { msalInstance, useDevAuth } from "../auth/config";
import { getDevRole, switchDevRole } from "../auth/devRole";
import { strings } from "../lib/strings";

export function Shell({ children }: { children: ReactNode }) {
  const me = useMe();
  const navigate = useNavigate();
  const location = useLocation();
  const isAdminArea = location.pathname.startsWith("/admin");

  async function logout() {
    if (useDevAuth || !msalInstance) {
      navigate("/login");
      return;
    }
    const account = msalInstance.getActiveAccount() ?? msalInstance.getAllAccounts()[0];
    await msalInstance.logoutRedirect({ account: account ?? undefined });
  }

  return (
    <div className="min-h-screen">
      <header className="sticky top-0 z-20 border-b border-line/80 bg-white/80 backdrop-blur-md">
        <div className="mx-auto flex w-full max-w-[1600px] items-center justify-between px-6 py-4 md:px-10">
          <Link to="/" className="text-xl font-semibold tracking-tight text-ink">
            {strings.appName}
          </Link>
          <div className="flex items-center gap-4 text-sm text-muted">
            {me.data && (
              <span className="hidden sm:inline">
                {me.data.displayName}
                {me.data.cohort ? ` · ${me.data.cohort.name}` : ""}
              </span>
            )}
            {me.data?.role === "Admin" && (
              <Link
                to={isAdminArea ? "/" : "/admin"}
                className="rounded-lg px-3 py-1.5 font-medium text-ink transition hover:bg-panel-2"
              >
                {isAdminArea ? strings.adminLab : strings.admin}
              </Link>
            )}
            {useDevAuth && (
              <button
                type="button"
                onClick={() => switchDevRole(getDevRole() === "Admin" ? "Student" : "Admin")}
                className="rounded-lg px-3 py-1.5 font-medium text-ink transition hover:bg-panel-2"
              >
                {getDevRole() === "Admin" ? strings.loginDevAction : strings.loginDevAdmin}
              </button>
            )}
            <button
              type="button"
              onClick={() => void logout()}
              className="rounded-lg px-3 py-1.5 font-medium text-ink transition hover:bg-panel-2"
            >
              {strings.logout}
            </button>
          </div>
        </div>
      </header>
      <main className="mx-auto w-full max-w-[1600px] px-6 py-6 md:px-10 md:py-10">{children}</main>
    </div>
  );
}
