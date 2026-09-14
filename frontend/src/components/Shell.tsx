import type { ReactNode } from "react";
import { Link, useLocation, useNavigate } from "react-router-dom";
import { useMe } from "../api/hooks";
import { msalInstance, useDevAuth } from "../auth/config";
import { getDevRole, switchDevRole } from "../auth/devRole";
import { ThemeToggle } from "../theme/ThemeToggle";
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
        className="sr-only focus:not-sr-only focus:absolute focus:left-4 focus:top-4 focus:z-50 focus:rounded-xl focus:bg-accent focus:px-4 focus:py-2 focus:text-sm focus:font-semibold focus:text-on-accent"
      >
        {strings.skipToContent}
      </a>
      <header className="sticky top-0 z-20 border-b border-line/80 bg-bg/75 backdrop-blur-xl">
        <div className="mx-auto flex w-full max-w-[1600px] flex-wrap items-center justify-between gap-3 px-4 py-3.5 sm:px-6 md:px-10">
          <Link to="/" className="flex items-center gap-2.5 text-ink">
            <span className="inline-flex size-8 items-center justify-center rounded-lg bg-accent/12 text-accent">
              <BrandMark />
            </span>
            <span className="text-lg font-semibold tracking-tight">{strings.appName}</span>
          </Link>
          <div className="flex flex-wrap items-center gap-1 text-sm text-muted sm:gap-2">
            {me.data && (
              <span className="hidden rounded-full border border-line bg-panel-2/70 px-3 py-1.5 sm:inline">
                {me.data.displayName}
                {me.data.cohort ? ` · ${me.data.cohort.name}` : ""}
              </span>
            )}
            {me.data?.role === "Admin" && (
              <Link
                to={isAdminArea ? "/" : "/admin"}
                className="min-h-11 rounded-lg px-3 py-1.5 font-medium text-ink transition hover:bg-panel-2 hover:text-accent"
              >
                {isAdminArea ? strings.adminLab : strings.admin}
              </Link>
            )}
            {useDevAuth && (
              <button
                type="button"
                onClick={() => switchDevRole(getDevRole() === "Admin" ? "Student" : "Admin")}
                className="min-h-11 rounded-lg px-3 py-1.5 font-medium text-ink transition hover:bg-panel-2 hover:text-accent"
              >
                {getDevRole() === "Admin" ? strings.loginDevAction : strings.loginDevAdmin}
              </button>
            )}
            <ThemeToggle />
            <button
              type="button"
              onClick={() => void logout()}
              className="min-h-11 rounded-lg px-3 py-1.5 font-medium text-ink transition hover:bg-panel-2 hover:text-accent"
            >
              {strings.logout}
            </button>
          </div>
        </div>
      </header>
      <main id="sadrzaj" className="mx-auto w-full max-w-[1600px] px-4 py-6 sm:px-6 md:px-10">
        {children}
      </main>
    </div>
  );
}

function BrandMark() {
  return (
    <svg viewBox="0 0 20 20" className="size-4" aria-hidden="true" fill="none">
      <path
        d="M3 10h1.6l1.2-4 1.8 8 1.6-6.5 1.5 4.5H13.5"
        stroke="currentColor"
        strokeWidth="1.7"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
      <path
        d="M15.2 7.2a4.4 4.4 0 0 1 0 5.6M17.2 5.4a7 7 0 0 1 0 9.2"
        stroke="currentColor"
        strokeWidth="1.7"
        strokeLinecap="round"
      />
    </svg>
  );
}
