import type { ReactNode } from "react";
import { Navigate } from "react-router-dom";
import { useMe } from "../../api/hooks";
import { strings } from "../../lib/strings";

export function AdminGate({ children }: { children: ReactNode }) {
  const me = useMe();

  if (me.isPending) {
    return <p className="p-8 text-sm text-muted">{strings.loading}</p>;
  }

  if (me.error || me.data?.role !== "Admin") {
    return <Navigate to="/" replace />;
  }

  return children;
}
