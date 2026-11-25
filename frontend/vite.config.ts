import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// Tarayıcı yalnızca bu adresle konuşur; /api istekleri backend'e iletilir. Böylece CORS ayarı gerekmez.
const proxy = {
  '/api': { target: process.env.NOVABANK_API ?? 'http://localhost:5080', changeOrigin: true },
}

export default defineConfig({
  plugins: [react()],
  server: { port: 5173, strictPort: true, proxy },
  preview: { proxy },
})
