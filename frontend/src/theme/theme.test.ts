import { describe, expect, it } from "vitest";
import { resolveTheme, toggleTheme } from "./theme";

describe("theme", () => {
  it("keeps a stored light or dark value", () => {
    expect(resolveTheme("light")).toBe("light");
    expect(resolveTheme("dark")).toBe("dark");
  });

  it("falls back to dark when nothing is stored", () => {
    expect(resolveTheme(null)).toBe("dark");
    expect(resolveTheme("system")).toBe("dark");
  });

  it("toggles between dark and light", () => {
    expect(toggleTheme("dark")).toBe("light");
    expect(toggleTheme("light")).toBe("dark");
  });
});
