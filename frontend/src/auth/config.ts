import { type Configuration, LogLevel, PublicClientApplication } from "@azure/msal-browser";

export const useDevAuth = import.meta.env.VITE_USE_DEV_AUTH === "true";

if (import.meta.env.PROD && useDevAuth) {
  throw new Error("VITE_USE_DEV_AUTH ne smije biti uključen u produkcijskom buildu.");
}

export const apiBaseUrl = import.meta.env.VITE_API_BASE_URL.replace(/\/$/, "");

export const apiScope = import.meta.env.VITE_API_SCOPE;

const msalConfig: Configuration = {
  auth: {
    clientId: import.meta.env.VITE_ENTRA_CLIENT_ID || "00000000-0000-0000-0000-000000000000",
    authority: import.meta.env.VITE_ENTRA_TENANT_ID
      ? `https://login.microsoftonline.com/${import.meta.env.VITE_ENTRA_TENANT_ID}`
      : "https://login.microsoftonline.com/common",
    redirectUri: window.location.origin
  },
  cache: {
    cacheLocation: "sessionStorage"
  },
  system: {
    loggerOptions: {
      logLevel: LogLevel.Warning,
      loggerCallback: () => undefined
    }
  }
};

export const msalInstance = useDevAuth ? null : new PublicClientApplication(msalConfig);
