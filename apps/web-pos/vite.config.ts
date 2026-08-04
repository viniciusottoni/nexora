import { defineConfig } from 'vite';

function proxyTo(target: string, prefix?: string) {
  return {
    target,
    changeOrigin: true,
    secure: false,
    ws: true,
    ...(prefix ? { rewrite: (path: string) => path.replace(new RegExp(`^${prefix}`), '') } : {}),
  };
}

export default defineConfig({
  server: {
    port: 5175,
    strictPort: true,
    proxy: {
      '/v1': proxyTo('http://localhost:5000'),
      '/hubs': proxyTo('http://localhost:5000'),
      // E-08 (US-081 §7) — POST /v1/notifications/subscribe só existe no host cloud, nunca no edge
      // (ver notifications/push-notifications.ts). Mesmo mapeamento /cloud de apps/web-admin/vite.config.ts.
      '/cloud': proxyTo('http://localhost:5100', '/cloud'),
    },
  },
  preview: {
    port: 5175,
    strictPort: true,
    proxy: {
      '/v1': proxyTo('http://localhost:5000'),
      '/hubs': proxyTo('http://localhost:5000'),
      '/cloud': proxyTo('http://localhost:5100', '/cloud'),
    },
  },
});
