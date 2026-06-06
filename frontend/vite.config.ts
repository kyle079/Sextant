import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

// In dev, the backend runs on HTTPS at 7060 (dotnet run --launch-profile https).
// The auth callback goes directly to https://localhost:7060 (Eve SSO redirects there),
// so for full auth testing run: npm run build && dotnet run --launch-profile https
// For frontend-only development (no auth flow), use: npm run dev
const BACKEND = 'https://localhost:7060';

export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      '/auth': { target: BACKEND, secure: false, changeOrigin: true },
      '/chain': { target: BACKEND, secure: false },
      '/sigs': { target: BACKEND, secure: false },
      '/mass': { target: BACKEND, secure: false },
      '/rolling': { target: BACKEND, secure: false },
      '/pi': { target: BACKEND, secure: false },
      '/fittings': { target: BACKEND, secure: false },
      '/sites': { target: BACKEND, secure: false },
      '/members': { target: BACKEND, secure: false },
      '/structures': { target: BACKEND, secure: false },
      '/kills': { target: BACKEND, secure: false },
      '/activity': { target: BACKEND, secure: false },
      '/admin': { target: BACKEND, secure: false },
      '/health': { target: BACKEND, secure: false },
      '/hub': { target: BACKEND, secure: false, ws: true },
    },
  },
  build: {
    outDir: '../backend/wwwroot',
    emptyOutDir: true,
  },
});
