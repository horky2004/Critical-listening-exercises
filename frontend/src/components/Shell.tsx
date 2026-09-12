import type { ReactNode } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useMe } from "../api/hooks";
import { msalInstance, useDevAuth } from "../auth/config";
import { strings } from "../lib/strings";

export function Shell({ children }: { children: ReactNode }) {
  const me = useMe();
  const navigate = useNavigate();

  async function logout() {
    if (useDevAuth || !msalInstance) {
      navigate("/login");
      return;
    }
    const account = msalInstance.getActiveAccount() ?? msalInstance.getAllAccounts()[0];
    await msalInstance.logoutRedirect({ account: account ?? undefined });
  }

  return (
    <div className="min-h-screen bg-bg">
      <header className="border-b border-line">
        <div className="mx-auto flex max-w-6xl items-center justify-between px-6 py-4">
          <Link to="/" className="text-sm font-semibold tracking-wide text-ink">
            {strings.appName}
          </Link>
          <div className="flex items-center gap-4 text-sm text-muted">
            {me.data && (
              <span>
                {me.data.displayName}
                {me.data.cohort ? ` · ${me.data.cohort.name}` : ""}
              </span>
            )}
            <button type="button" onClick={() => void logout()} className="hover:text-ink">
              {strings.logout}
            </button>
          </div>
        </div>
      </header>
      <main className="mx-auto max-w-6xl px-6 py-8">{children}</main>
    </div>
  );
}
