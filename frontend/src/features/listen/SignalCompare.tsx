import { strings } from "../../lib/strings";

export function SignalCompare({
  source,
  disabled,
  onSelect
}: {
  source: boolean;
  disabled?: boolean;
  onSelect: (source: boolean) => void;
}) {
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
      <p className="mt-2 text-center text-sm text-muted">{strings.listenShortcuts}</p>
    </div>
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
          ? "border-accent bg-accent text-white shadow-[0_10px_24px_rgba(15,118,110,0.18)]"
          : "border-line bg-panel text-ink hover:border-accent/40 hover:bg-accent/5"
      }`}
    >
      {label}
    </button>
  );
}
