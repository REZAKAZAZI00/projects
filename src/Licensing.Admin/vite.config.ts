import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: 'http://localhost:5079',
        changeOrigin: true,
        secure: false
      },
      '/health': 'http://localhost:5079'
    }
  },
  build: {
    outDir: '../Licensing.Api/wwwroot',
    emptyOutDir: true
  }
});
