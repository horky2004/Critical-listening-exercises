import { useState } from "react";
import { strings } from "../lib/strings";
import { persistTheme, readTheme, toggleTheme, type Theme } from "./theme";

export function ThemeToggle({ className = "" }: { className?: string }) {
  const [theme, setTheme] = useState<Theme>(() =>
    typeof document === "undefined" ? "dark" : readDocumentTheme()
  );
  const next = toggleTheme(theme);
  const label = next === "light" ? strings.themeToLight : strings.themeToDark;

  return (
    <button
      type="button"
      className={`inline-flex min-h-11 min-w-11 items-center justify-center rounded-lg text-ink transition hover:bg-panel-2 hover:text-accent ${className}`}
      aria-label={label}
      title={label}
      onClick={() => {
        persistTheme(next);
        setTheme(next);
      }}
    >
      {theme === "dark" ? <SunIcon /> : <MoonIcon />}
    </button>
  );
}

function readDocumentTheme(): Theme {
  return document.documentElement.dataset.theme === "light" ? "light" : readTheme();
}

function SunIcon() {
  return (
    <svg viewBox="0 0 20 20" className="size-5" aria-hidden="true" fill="none">
      <circle cx="10" cy="10" r="3.2" stroke="currentColor" strokeWidth="1.7" />
      <path
        d="M10 2.4v1.6M10 16v1.6M2.4 10h1.6M16 10h1.6M4.6 4.6l1.1 1.1M14.3 14.3l1.1 1.1M4.6 15.4l1.1-1.1M14.3 5.7l1.1-1.1"
        stroke="currentColor"
        strokeWidth="1.7"
        strokeLinecap="round"
      />
    </svg>
  );
}

function MoonIcon() {
  return (
    <svg viewBox="0 0 20 20" className="size-5" aria-hidden="true" fill="none">
      <path
        d="M14.8 12.4A5.6 5.6 0 0 1 7.6 5.2 5.7 5.7 0 1 0 14.8 12.4Z"
        stroke="currentColor"
        strokeWidth="1.7"
        strokeLinejoin="round"
      />
    </svg>
  );
}
