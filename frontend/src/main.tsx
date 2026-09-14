import { MsalProvider } from "@azure/msal-react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { BrowserRouter } from "react-router-dom";
import { App } from "./App";
import { ErrorBoundary } from "./components/ErrorBoundary";
import { msalInstance } from "./auth/config";
import "./styles/index.css";

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 15_000,
      retry: 1
    }
  }
});

async function boot() {
  if (msalInstance) {
    await msalInstance.initialize();
    const account = msalInstance.getActiveAccount() ?? msalInstance.getAllAccounts()[0];
    if (account) {
      msalInstance.setActiveAccount(account);
    }
  }

  const root = document.getElementById("root");
  if (!root) {
    throw new Error("root nije pronaden");
  }

  const tree = (
    <StrictMode>
      <ErrorBoundary>
        <QueryClientProvider client={queryClient}>
          <BrowserRouter>
            <App />
          </BrowserRouter>
        </QueryClientProvider>
      </ErrorBoundary>
    </StrictMode>
  );

  createRoot(root).render(
    msalInstance ? <MsalProvider instance={msalInstance}>{tree}</MsalProvider> : tree
  );
}

void boot();
