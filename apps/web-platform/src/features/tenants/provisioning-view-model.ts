export function deriveSlugSuggestion(
  name: string,
  currentSlug: string,
  manuallyEdited: boolean,
): string {
  if (manuallyEdited) return currentSlug;
  return name
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '')
    .slice(0, 64)
    .replace(/-+$/g, '');
}

export function maskInstallationCommand(command: string): string {
  return command.replace(/(--token=)[^\s]+/, '$1••••••••');
}
