// Validação de contraste WCAG AA de branding — portado de packages/domain/src/branding/contrast.ts
// para dentro de @nexora/contracts na remoção do backend TypeScript (ADR-036/037/038), já que este
// pacote é consumido tanto pelo frontend (packages/ui/src/branding/contrast-advisor.tsx, preview
// ao vivo no editor de marca) quanto era consumido pelo backend antigo. A regra de negócio
// equivalente agora vive em C# em Nexora.Domain.Platform.BrandingContrast (backend/src/
// Nexora.Domain/Platform/BrandingContrast.cs) — mantenha as duas implementações em sincronia
// se a fórmula de contraste mudar.

export interface BrandingColorSet {
  readonly primary: string;
  readonly surface: string;
  readonly onPrimary: string;
}

export interface ContrastIssue {
  readonly pair: 'primary/surface' | 'onPrimary/primary';
  readonly ratio: number;
  readonly suggested: string;
}

export interface BrandingContrastResult {
  readonly valid: boolean;
  readonly minimumRatio: 4.5;
  readonly issues: readonly ContrastIssue[];
}

const HEX_COLOR = /^#([0-9a-f]{6})$/i;
const MINIMUM_AA_RATIO = 4.5;

export function contrastRatio(foreground: string, background: string): number {
  const bright = relativeLuminance(parseHex(foreground));
  const dark = relativeLuminance(parseHex(background));
  const [lighter, darker] = bright >= dark ? [bright, dark] : [dark, bright];
  return round((lighter + 0.05) / (darker + 0.05), 4);
}

export function suggestAccessibleColor(
  foreground: string,
  background: string,
  minimumRatio = MINIMUM_AA_RATIO,
): string {
  const source = parseHex(foreground);
  parseHex(background);
  if (contrastRatio(foreground, background) >= minimumRatio) return toHex(source);

  const candidates = [
    [0, 0, 0],
    [255, 255, 255],
  ]
    .map((target) => closestAccessibleBlend(source, target, background, minimumRatio))
    .filter((candidate): candidate is readonly number[] => candidate !== undefined)
    .sort((left, right) => distance(source, left) - distance(source, right));

  const suggestion = candidates[0];
  if (!suggestion) throw new Error('Não existe variação com o contraste solicitado');
  return toHex(suggestion);
}

export function validateBrandingContrast(colors: BrandingColorSet): BrandingContrastResult {
  const pairs = [
    { pair: 'primary/surface' as const, foreground: colors.primary, background: colors.surface },
    {
      pair: 'onPrimary/primary' as const,
      foreground: colors.onPrimary,
      background: colors.primary,
    },
  ];
  const issues = pairs.flatMap(({ pair, foreground, background }) => {
    const ratio = contrastRatio(foreground, background);
    return ratio >= MINIMUM_AA_RATIO
      ? []
      : [{ pair, ratio, suggested: suggestAccessibleColor(foreground, background) }];
  });
  return { valid: issues.length === 0, minimumRatio: 4.5, issues };
}

function closestAccessibleBlend(
  source: readonly number[],
  target: readonly number[],
  background: string,
  minimumRatio: number,
): readonly number[] | undefined {
  for (let step = 1; step <= 1_000; step += 1) {
    const weight = step / 1_000;
    const candidate = source.map((channel, index) =>
      Math.round(channel + ((target[index] ?? channel) - channel) * weight),
    );
    if (contrastRatio(toHex(candidate), background) >= minimumRatio) return candidate;
  }
  return undefined;
}

function parseHex(value: string): readonly number[] {
  const match = HEX_COLOR.exec(value);
  if (!match?.[1]) throw new Error('Cor hexadecimal inválida');
  return [0, 2, 4].map((offset) => Number.parseInt(match[1]!.slice(offset, offset + 2), 16));
}

function toHex(rgb: readonly number[]): string {
  return `#${rgb
    .map((channel) => channel.toString(16).padStart(2, '0'))
    .join('')
    .toUpperCase()}`;
}

function relativeLuminance(rgb: readonly number[]): number {
  const [red = 0, green = 0, blue = 0] = rgb.map((channel) => {
    const normalized = channel / 255;
    return normalized <= 0.03928 ? normalized / 12.92 : ((normalized + 0.055) / 1.055) ** 2.4;
  });
  return 0.2126 * red + 0.7152 * green + 0.0722 * blue;
}

function distance(left: readonly number[], right: readonly number[]): number {
  return Math.sqrt(
    left.reduce((sum, channel, index) => sum + (channel - (right[index] ?? channel)) ** 2, 0),
  );
}

function round(value: number, places: number): number {
  const scale = 10 ** places;
  return Math.round(value * scale) / scale;
}
