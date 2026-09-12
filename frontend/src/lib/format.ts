export function formatHz(frequencyHz: number): string {
  return frequencyHz >= 1000 ? `${frequencyHz / 1000} kHz` : `${frequencyHz} Hz`;
}

export function formatGain(gainDb: number): string {
  return `${gainDb > 0 ? "+" : ""}${gainDb} dB`;
}

export function percent(value: number): string {
  return `${value.toFixed(1)}%`;
}

export function scoreLine(correct: number, total: number, percentage: number): string {
  return `${correct} / ${total}  ·  ${percent(percentage)}`;
}
