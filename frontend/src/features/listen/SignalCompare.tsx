import { strings } from "../../lib/strings";

export function SignalCompare({
  source,
  disabled,
  onSelect,
  variant = "default",
  hint
}: {
  source: boolean;
  disabled?: boolean;
  onSelect: (source: boolean) => void;
  variant?: "default" | "pill";
  hint?: string;
}) {
  if (variant === "pill") {
    return (
      <div>
        <div className="mx-auto grid max-w-xs grid-cols-2 rounded-full bg-panel-2 p-1">
          <PillButton
            label={strings.source}
            active={source}
            disabled={disabled}
            onClick={() => onSelect(true)}
          />
          <PillButton
            label={strings.processed}
            active={!source}
            disabled={disabled}
            onClick={() => onSelect(false)}
          />
        </div>
        <p className="mt-3 text-center text-sm text-muted">{hint ?? strings.listenShortcuts}</p>
      </div>
    );
  }

  return (
    <div>
      <div className="grid grid-cols-2 gap-3">
        <CompareButton
          label={strings.source}
          active={source}
          disabled={disabled}
          onClick={() => onSelect(true)}
        />
        <CompareButton
          label={strings.processed}
          active={!source}
          disabled={disabled}
          onClick={() => onSelect(false)}
        />
      </div>
      <p className="mt-2 text-center text-sm text-muted">{hint ?? strings.listenShortcuts}</p>
    </div>
  );
}

function PillButton({
  label,
  active,
  disabled,
  onClick
}: {
  label: string;
  active: boolean;
  disabled?: boolean;
  onClick: () => void;
}) {
  return (
    <button
      type="button"
      disabled={disabled}
      aria-pressed={active}
      onClick={onClick}
      className={`rounded-full px-5 py-2.5 text-sm font-semibold tracking-tight transition duration-150 disabled:cursor-not-allowed disabled:opacity-40 ${
        active ? "bg-accent text-on-accent shadow-sm" : "bg-transparent text-muted hover:text-ink"
      }`}
    >
      {label}
    </button>
  );
}

function CompareButton({
  label,
  active,
  disabled,
  onClick
}: {
  label: string;
  active: boolean;
  disabled?: boolean;
  onClick: () => void;
}) {
  return (
    <button
      type="button"
      disabled={disabled}
      aria-pressed={active}
      onClick={onClick}
      className={`rounded-2xl border px-5 py-5 text-lg font-semibold tracking-tight transition duration-150 disabled:cursor-not-allowed disabled:opacity-40 ${
        active
          ? "border-accent bg-accent text-on-accent shadow-[var(--lift-toggle)]"
          : "border-line bg-panel text-ink hover:border-accent/40 hover:bg-accent/10"
      }`}
    >
      {label}
    </button>
  );
}
