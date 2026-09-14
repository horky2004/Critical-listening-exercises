import { useEffect, useState, type ReactNode } from "react";
import { mnemonicFor } from "../features/intro/frequencies";
import { formatHz } from "../lib/format";
import { strings } from "../lib/strings";

export function EqExerciseLayout({
  frequenciesHz,
  children
}: {
  frequenciesHz: readonly number[] | null | undefined;
  children: ReactNode;
}) {
  const frequencies = uniqueSorted(frequenciesHz);
  if (frequencies.length === 0) {
    return children;
  }

  return (
    <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:gap-5">
      <MnemonicCheatSheet frequenciesHz={frequencies} />
      <div className="min-w-0 w-full flex-1">{children}</div>
    </div>
  );
}

export function MnemonicCheatSheet({ frequenciesHz }: { frequenciesHz: readonly number[] }) {
  const rows = uniqueSorted(frequenciesHz).map((hz) => ({
    hz,
    sound: mnemonicFor(hz).sound
  }));
  const desktop = useDesktopLayout();
  const [open, setOpen] = useState(false);
  const expanded = desktop || open;

  if (rows.length === 0) {
    return null;
  }

  return (
    <aside className="w-full shrink-0 lg:absolute lg:top-40 lg:right-0 xl:right-12 lg:order-last lg:w-36 xl:w-44">
      <section className="rounded-2xl border border-line bg-panel shadow-[0_10px_30px_rgba(21,32,51,0.04)]">
        {desktop ? (
          <h2 className="border-b border-line px-2 py-3 text-center text-xs font-semibold leading-snug tracking-tight">
            {strings.mnemonicCheatSheet}
          </h2>
        ) : (
          <button
            type="button"
            aria-expanded={expanded}
            aria-controls="mnemonic-cheat-sheet"
            onClick={() => setOpen((current) => !current)}
            className="flex w-full items-center justify-between gap-3 px-4 py-3 text-left"
          >
            <span className="text-sm font-semibold tracking-tight">{strings.mnemonicCheatSheet}</span>
            <Hamburger open={expanded} />
          </button>
        )}
        {expanded && (
          <table id="mnemonic-cheat-sheet" className="w-full border-collapse text-sm">
            <thead>
              <tr className="border-b border-line text-left text-[10px] font-semibold uppercase tracking-[0.12em] text-muted">
                <th className="px-2 xl:px-3 py-2 font-semibold">{strings.mnemonicFrequency}</th>
                <th className="px-2 xl:px-3 py-2 font-semibold">{strings.mnemonicVoice}</th>
              </tr>
            </thead>
            <tbody>
              {rows.map((row) => (
                <tr key={row.hz} className="border-b border-line/70 last:border-b-0">
                  <td className="tabular px-3 py-2 font-medium text-ink">{formatHz(row.hz)}</td>
                  <td className="px-3 py-2 font-mono text-base font-semibold tracking-wide text-accent">
                    {row.sound}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </section>
    </aside>
  );
}

function uniqueSorted(frequenciesHz: readonly number[] | null | undefined): number[] {
  return [...new Set(frequenciesHz ?? [])].sort((a, b) => a - b);
}

function useDesktopLayout(): boolean {
  const [desktop, setDesktop] = useState(() =>
    typeof window !== "undefined" && window.matchMedia("(min-width: 1024px)").matches
  );

  useEffect(() => {
    const media = window.matchMedia("(min-width: 1024px)");
    const sync = () => setDesktop(media.matches);
    sync();
    media.addEventListener("change", sync);
    return () => media.removeEventListener("change", sync);
  }, []);

  return desktop;
}

function Hamburger({ open }: { open: boolean }) {
  return (
    <span className="relative block size-4 shrink-0 text-muted" aria-hidden="true">
      <span
        className={`absolute left-0 block h-0.5 w-4 rounded-full bg-current transition ${
          open ? "top-1.5 rotate-45" : "top-0.5"
        }`}
      />
      <span
        className={`absolute left-0 top-1.5 block h-0.5 w-4 rounded-full bg-current transition ${
          open ? "opacity-0" : "opacity-100"
        }`}
      />
      <span
        className={`absolute left-0 block h-0.5 w-4 rounded-full bg-current transition ${
          open ? "top-1.5 -rotate-45" : "top-2.5"
        }`}
      />
    </span>
  );
}
