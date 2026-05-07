import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      '/api': 'http://localhost:8080',
      '/auth': 'http://localhost:8080',
      '/chain': 'http://localhost:8080',
      '/sigs': 'http://localhost:8080',
      '/mass': 'http://localhost:8080',
      '/rolling': 'http://localhost:8080',
      '/pi': 'http://localhost:8080',
      '/fittings': 'http://localhost:8080',
      '/sites': 'http://localhost:8080',
      '/members': 'http://localhost:8080',
      '/structures': 'http://localhost:8080',
      '/kills': 'http://localhost:8080',
      '/activity': 'http://localhost:8080',
      '/admin': 'http://localhost:8080',
      '/health': 'http://localhost:8080',
      '/hub': { target: 'http://localhost:8080', ws: true },
    },
  },
  build: {
    outDir: '../backend/wwwroot',
    emptyOutDir: true,
  },
});
