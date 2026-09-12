import { InteractionStatus } from "@azure/msal-browser";
import { useIsAuthenticated, useMsal } from "@azure/msal-react";
import type { ReactNode } from "react";
import { Navigate, useLocation } from "react-router-dom";
import { strings } from "../lib/strings";
import { useDevAuth } from "./config";

export function AuthGate({ children }: { children: ReactNode }) {
  const location = useLocation();

  if (useDevAuth) {
    return children;
  }

  return <EntraGate locationPath={`${location.pathname}${location.search}`}>{children}</EntraGate>;
}

function EntraGate({ children, locationPath }: { children: ReactNode; locationPath: string }) {
  const { inProgress } = useMsal();
  const authenticated = useIsAuthenticated();

  if (inProgress !== InteractionStatus.None) {
    return <p className="p-8 text-muted">{strings.loading}</p>;
  }

  if (!authenticated) {
    return <Navigate to="/login" replace state={{ from: locationPath }} />;
  }

  return children;
}
