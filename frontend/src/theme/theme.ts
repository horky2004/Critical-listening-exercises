export type Theme = "light" | "dark";

export const themeStorageKey = "cll-theme";

export function resolveTheme(stored: string | null | undefined): Theme {
  return stored === "light" || stored === "dark" ? stored : "dark";
}

export function readTheme(): Theme {
  try {
    return resolveTheme(localStorage.getItem(themeStorageKey));
  } catch {
    return "dark";
  }
}

export function applyTheme(theme: Theme) {
  document.documentElement.dataset.theme = theme;
}

export function persistTheme(theme: Theme) {
  applyTheme(theme);
  try {
    localStorage.setItem(themeStorageKey, theme);
  } catch {
    /* private mode */
  }
}

export function toggleTheme(current: Theme): Theme {
  return current === "dark" ? "light" : "dark";
}
