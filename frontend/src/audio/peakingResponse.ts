const SAMPLE_RATE = 44100;

export function peakingMagnitudeDb(
  frequencyHz: number,
  band: { frequencyHz: number; gainDb: number; q: number }
): number {
  const nyquist = SAMPLE_RATE / 2;
  const freq = Math.min(Math.max(frequencyHz, 1), nyquist - 1);
  const f0 = Math.min(Math.max(band.frequencyHz, 1), nyquist - 1);
  const q = Math.max(band.q, 0.01);
  const A = 10 ** (band.gainDb / 40);
  const w0 = (2 * Math.PI * f0) / SAMPLE_RATE;
  const cos0 = Math.cos(w0);
  const alpha = Math.sin(w0) / (2 * q);

  const b0 = 1 + alpha * A;
  const b1 = -2 * cos0;
  const b2 = 1 - alpha * A;
  const a0 = 1 + alpha / A;
  const a1 = -2 * cos0;
  const a2 = 1 - alpha / A;

  const w = (2 * Math.PI * freq) / SAMPLE_RATE;
  const cos1 = Math.cos(w);
  const sin1 = Math.sin(w);
  const cos2 = Math.cos(2 * w);
  const sin2 = Math.sin(2 * w);

  const numRe = b0 + b1 * cos1 + b2 * cos2;
  const numIm = -(b1 * sin1 + b2 * sin2);
  const denRe = a0 + a1 * cos1 + a2 * cos2;
  const denIm = -(a1 * sin1 + a2 * sin2);
  const mag2 = (numRe * numRe + numIm * numIm) / (denRe * denRe + denIm * denIm);
  return 10 * Math.log10(Math.max(mag2, 1e-12));
}

export function logFrequencyPoints(fromHz: number, toHz: number, count: number): number[] {
  const start = Math.log10(fromHz);
  const end = Math.log10(toHz);
  return Array.from({ length: count }, (_, index) => 10 ** (start + ((end - start) * index) / (count - 1)));
}
