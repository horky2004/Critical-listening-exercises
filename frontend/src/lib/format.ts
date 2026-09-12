export function percent(value: number): string {
  return `${value.toFixed(1)}%`;
}

export function scoreLine(correct: number, total: number, percentage: number): string {
  return `${correct} / ${total}  ·  ${percent(percentage)}`;
}
