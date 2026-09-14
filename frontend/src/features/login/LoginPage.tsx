import type { ReactNode } from "react";
import { InteractionStatus } from "@azure/msal-browser";
import { useIsAuthenticated, useMsal } from "@azure/msal-react";
import { Navigate, useLocation, useNavigate } from "react-router-dom";
import { Button } from "../../components/Button";
import { apiScope, useDevAuth } from "../../auth/config";
import { setDevRole } from "../../auth/devRole";
import { strings } from "../../lib/strings";
import { ThemeToggle } from "../../theme/ThemeToggle";

export function LoginPage() {
  const location = useLocation();
  const from = (location.state as { from?: string } | null)?.from ?? "/";

  const navigate = useNavigate();

  if (useDevAuth) {
    return (
      <LoginLayout>
        <p className="text-sm text-accent-dim">{strings.loginDevNote}</p>
        <Button
          className="w-full"
          onClick={() => {
            setDevRole("Student");
            navigate(from.startsWith("/admin") ? "/" : from);
          }}
        >
          {strings.loginDevAction}
        </Button>
        <Button
          variant="ghost"
          className="w-full"
          onClick={() => {
            setDevRole("Admin");
            navigate("/admin");
          }}
        >
          {strings.loginDevAdmin}
        </Button>
      </LoginLayout>
    );
  }

  return <EntraLogin from={from} />;
}

function EntraLogin({ from }: { from: string }) {
  const { instance, inProgress } = useMsal();
  const authenticated = useIsAuthenticated();

  if (authenticated) {
    return <Navigate to={from} replace />;
  }

  return (
    <LoginLayout>
      <Button
        className="w-full"
        disabled={inProgress !== InteractionStatus.None}
        onClick={() => void instance.loginRedirect({ scopes: [apiScope] })}
      >
        {strings.loginAction}
      </Button>
    </LoginLayout>
  );
}

function LoginLayout({ children }: { children: ReactNode }) {
  return (
    <div className="relative flex min-h-screen items-center justify-center px-6">
      <ThemeToggle className="absolute right-4 top-4 sm:right-6 sm:top-6" />
      <div className="w-full max-w-md space-y-7 rounded-3xl border border-line bg-panel/90 p-10 shadow-[var(--elevated-shadow)] backdrop-blur-xl">
        <div>
          <p className="text-xs font-semibold uppercase tracking-[0.22em] text-accent">{strings.loginTitle}</p>
          <h1 className="mt-3 text-2xl font-semibold tracking-tight">{strings.appName}</h1>
          <p className="mt-3 text-sm leading-6 text-muted">{strings.loginBody}</p>
        </div>
        {children}
      </div>
    </div>
  );
}
