import { defineConfig } from 'vite';

function proxyTo(target: string, prefix: string) {
  return {
    target,
    changeOrigin: true,
    secure: false,
    ws: true,
    rewrite: (path: string) => path.replace(new RegExp(`^${prefix}`), ''),
  };
}

export default defineConfig({
  server: {
    port: 5173,
    strictPort: true,
    proxy: {
      '/cloud': proxyTo('http://localhost:5100', '/cloud'),
      '/edge': proxyTo('http://localhost:5000', '/edge'),
    },
  },
  preview: {
    port: 5173,
    strictPort: true,
    proxy: {
      '/cloud': proxyTo('http://localhost:5100', '/cloud'),
      '/edge': proxyTo('http://localhost:5000', '/edge'),
    },
  },
});
