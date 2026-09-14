import { useEffect, useState } from "react";
import { audioFailureMessage } from "../../audio/audioErrors";
import { useCompressionAb } from "../../audio/useCompressionAb";
import { useListenHotkeys } from "../../audio/useListenHotkeys";
import { useListenVolume } from "../../audio/useListenVolume";
import { strings } from "../../lib/strings";
import { EqListenBar } from "./EqListenBar";

export type CompressionListenVariant = {
  variantSlug: string;
  label: string;
  url: string;
};

export function CompressionVariantListen({
  variants,
  hint
}: {
  variants: CompressionListenVariant[];
  hint?: string;
}) {
  const player = useCompressionAb();
  const [active, setActive] = useState(variants[0]?.variantSlug ?? "");
  const [playing, setPlaying] = useState(false);
  const [ready, setReady] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [volume, setVolume] = useListenVolume();
  const signature = variants.map((variant) => `${variant.variantSlug}:${variant.url}`).join("|");

  useEffect(() => {
    if (variants.length === 0) {
      return;
    }
    let cancelled = false;
    setReady(false);
    setError(null);
    setPlaying(false);
    const first = variants[0]?.variantSlug ?? "";
    setActive(first);
    void player
      .load(variants.map((variant) => ({ slug: variant.variantSlug, url: variant.url })))
      .then(() => {
        if (!cancelled) {
          player.setActive(first);
          player.setVolume(volume);
          setReady(true);
        }
      })
      .catch((cause: unknown) => {
        if (!cancelled) {
          setError(audioFailureMessage(cause));
        }
      });
    return () => {
      cancelled = true;
    };
  }, [player, signature]);

  function choose(slug: string) {
    setActive(slug);
    player.setActive(slug);
  }

  async function play() {
    const slug = active || variants[0]?.variantSlug;
    if (!slug) {
      return;
    }
    player.setActive(slug);
    player.setVolume(volume);
    try {
      await player.play();
      setPlaying(true);
    } catch (cause) {
      setError(audioFailureMessage(cause));
    }
  }

  function stop() {
    player.stop();
    setPlaying(false);
  }

  useListenHotkeys({
    enabled: ready,
    onTogglePlay: () => (playing ? stop() : void play())
  });

  return (
    <div className="space-y-5">
      <div className="grid gap-3 sm:grid-cols-2">
        {variants.map((variant) => (
          <button
            key={variant.variantSlug}
            type="button"
            onClick={() => choose(variant.variantSlug)}
            aria-pressed={active === variant.variantSlug}
            className={`min-h-11 rounded-2xl border px-5 py-4 text-left text-base font-semibold transition ${
              active === variant.variantSlug
                ? "border-accent bg-accent/10"
                : "border-line bg-panel hover:border-accent/40"
            }`}
          >
            {variant.label}
          </button>
        ))}
      </div>
      <EqListenBar
        playing={playing}
        disabled={!ready || variants.length === 0}
        error={error}
        volume={volume}
        onPlay={() => void play()}
        onStop={stop}
        onVolumeChange={(next) => {
          setVolume(next);
          player.setVolume(next);
        }}
      />
      <p className="text-center text-sm text-muted">{hint ?? strings.listenShortcutsCompression}</p>
    </div>
  );
}
