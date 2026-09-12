import { apiScope, msalInstance, useDevAuth } from "./config";

export async function getAccessToken(): Promise<string | null> {
  if (useDevAuth || !msalInstance) {
    return null;
  }

  const account = msalInstance.getActiveAccount() ?? msalInstance.getAllAccounts()[0];
  if (!account) {
    return null;
  }

  const result = await msalInstance.acquireTokenSilent({
    account,
    scopes: [apiScope]
  });
  return result.accessToken;
}
