export type BrandingAssetKind = 'LOGO_LIGHT' | 'LOGO_DARK' | 'FAVICON' | 'PWA_ICON';
export type BrandingAssetValidation = Readonly<{ valid: true } | { valid: false; reason: string }>;

const MAX_BYTES = 10_000_000;

export async function validateBrandingAssetFile(
  file: File,
  kind: BrandingAssetKind,
): Promise<BrandingAssetValidation> {
  if (file.size > MAX_BYTES) return { valid: false, reason: 'O arquivo deve ter no máximo 10 MB.' };
  if (kind === 'PWA_ICON' && file.type === 'image/svg+xml')
    return { valid: false, reason: 'Ícones PWA devem usar PNG ou WebP.' };
  if (!['image/svg+xml', 'image/png', 'image/jpeg', 'image/webp'].includes(file.type)) {
    return { valid: false, reason: 'Formato de imagem não aceito.' };
  }
  const bytes = new Uint8Array(await readFile(file));
  const valid =
    file.type === 'image/png'
      ? startsWith(bytes, [0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a])
      : file.type === 'image/jpeg'
        ? startsWith(bytes, [0xff, 0xd8, 0xff])
        : file.type === 'image/webp'
          ? ascii(bytes, 0, 4) === 'RIFF' && ascii(bytes, 8, 4) === 'WEBP'
          : new TextDecoder().decode(bytes.slice(0, 512)).trimStart().startsWith('<svg');
  return valid
    ? { valid: true }
    : { valid: false, reason: 'Conteúdo do arquivo não corresponde ao formato informado.' };
}

function startsWith(bytes: Uint8Array, signature: readonly number[]): boolean {
  return signature.every((value, index) => bytes[index] === value);
}

function ascii(bytes: Uint8Array, offset: number, length: number): string {
  return String.fromCharCode(...bytes.slice(offset, offset + length));
}

function readFile(file: File): Promise<ArrayBuffer> {
  if (typeof file.arrayBuffer === 'function') return file.arrayBuffer();
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.addEventListener('load', () => resolve(reader.result as ArrayBuffer));
    reader.addEventListener('error', () =>
      reject(reader.error ?? new Error('Falha ao ler arquivo.')),
    );
    reader.readAsArrayBuffer(file);
  });
}
