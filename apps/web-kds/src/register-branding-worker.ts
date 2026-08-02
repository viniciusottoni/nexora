export function registerBrandingWorker(): void {
  if ('serviceWorker' in navigator) void navigator.serviceWorker.register('/branding-sw.js');
}
