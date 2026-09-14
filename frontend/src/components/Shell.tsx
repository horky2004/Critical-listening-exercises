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
      <a
        href="#sadrzaj"
        className="sr-only focus:not-sr-only focus:absolute focus:left-4 focus:top-4 focus:z-50 focus:rounded-xl focus:bg-accent focus:px-4 focus:py-2 focus:text-sm focus:font-semibold focus:text-white"
      >
        {strings.skipToContent}
      </a>
      <header className="sticky top-0 z-20 border-b border-line/80 bg-white/80 backdrop-blur-md">
        <div className="mx-auto flex w-full max-w-[1600px] flex-wrap items-center justify-between gap-3 px-4 py-4 sm:px-6 md:px-10">
          <Link to="/" className="text-xl font-semibold tracking-tight text-ink">
            {strings.appName}
          </Link>
          <div className="flex flex-wrap items-center gap-2 text-sm text-muted sm:gap-4">
            {me.data && (
              <span className="hidden sm:inline">
                {me.data.displayName}
                {me.data.cohort ? ` · ${me.data.cohort.name}` : ""}
              </span>
            )}
            {me.data?.role === "Admin" && (
              <Link
                to={isAdminArea ? "/" : "/admin"}
                className="min-h-11 rounded-lg px-3 py-1.5 font-medium text-ink transition hover:bg-panel-2"
              >
                {isAdminArea ? strings.adminLab : strings.admin}
              </Link>
            )}
            {useDevAuth && (
              <button
                type="button"
                onClick={() => switchDevRole(getDevRole() === "Admin" ? "Student" : "Admin")}
                className="min-h-11 rounded-lg px-3 py-1.5 font-medium text-ink transition hover:bg-panel-2"
              >
                {getDevRole() === "Admin" ? strings.loginDevAction : strings.loginDevAdmin}
              </button>
            )}
            <button
              type="button"
              onClick={() => void logout()}
              className="min-h-11 rounded-lg px-3 py-1.5 font-medium text-ink transition hover:bg-panel-2"
            >
              {strings.logout}
            </button>
          </div>
        </div>
      </header>
      <main id="sadrzaj" className="mx-auto w-full max-w-[1600px] px-4 py-4 sm:px-6 md:px-10 ">
        {children}
      </main>
    </div>
  );
}
