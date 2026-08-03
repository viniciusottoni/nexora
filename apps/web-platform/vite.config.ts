import { defineConfig } from 'vite';

function proxyTo(target: string) {
  return {
    target,
    changeOrigin: true,
    secure: false,
    ws: true,
  };
}

export default defineConfig({
  server: {
    port: 5174,
    strictPort: true,
    proxy: {
      '/v1': proxyTo('http://localhost:5100'),
    },
  },
  preview: {
    port: 5174,
    strictPort: true,
    proxy: {
      '/v1': proxyTo('http://localhost:5100'),
    },
  },
});
