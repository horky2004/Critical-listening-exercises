import type { ReactNode } from "react";
import { InteractionStatus } from "@azure/msal-browser";
import { useIsAuthenticated, useMsal } from "@azure/msal-react";
import { Navigate, useLocation, useNavigate } from "react-router-dom";
import { Button } from "../../components/Button";
import { apiScope, useDevAuth } from "../../auth/config";
import { strings } from "../../lib/strings";

export function LoginPage() {
  const location = useLocation();
  const from = (location.state as { from?: string } | null)?.from ?? "/";

  const navigate = useNavigate();

  if (useDevAuth) {
    return (
      <LoginLayout>
        <p className="text-sm text-accent-dim">{strings.loginDevNote}</p>
        <Button className="w-full" onClick={() => navigate(from)}>
          {strings.loginDevAction}
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
    <div className="flex min-h-screen items-center justify-center px-6">
      <div className="w-full max-w-md space-y-7 rounded-3xl border border-line bg-panel p-10 shadow-[0_20px_50px_rgba(21,32,51,0.08)]">
        <div>
          <h1 className="mt-3 text-xl font-semibold tracking-tight">{strings.appName}</h1>
          <p className="mt-3 text-sm leading-6 text-muted">{strings.loginBody}</p>
        </div>
        {children}
      </div>
    </div>
  );
}
