export type FrequencyMnemonic = {
  hz: number;
  sound: string;
  asIn: string;
  hint: string;
};

export const FREQUENCY_MNEMONICS: Record<number, FrequencyMnemonic> = {
  125: { hz: 125, sound: "uu", asIn: "kao u „boot“", hint: "Topli bas, prsni ton" },
  250: { hz: 250, sound: "oh", asIn: "kao u „hot“", hint: "Puni, okrugli donji srednji pojas" },
  500: { hz: 500, sound: "aw", asIn: "kao u „law“", hint: "Tijelo tona u srednjem basu" },
  1000: { hz: 1000, sound: "ah", asIn: "kao u „father“", hint: "Nosivost i prisutnost" },
  2000: { hz: 2000, sound: "eh", asIn: "kao u „bed“", hint: "Definiranost i napad" },
  4000: { hz: 4000, sound: "ee", asIn: "kao u „see“", hint: "Prisutnost i razumljivost" },
  8000: { hz: 8000, sound: "ss", asIn: "kao u „hiss“", hint: "Zračnost i visoki šum" }
};

export function mnemonicFor(hz: number): FrequencyMnemonic {
  return FREQUENCY_MNEMONICS[hz] ?? { hz, sound: "—", asIn: "", hint: "" };
}
