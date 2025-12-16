import { fileURLToPath, URL } from 'node:url'

import { defineConfig } from 'vite'
import type { PluginOption } from 'vite'
import vue from '@vitejs/plugin-vue'
import vueDevTools from 'vite-plugin-vue-devtools'
import tailwindcss from '@tailwindcss/vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [vue(), vueDevTools(), tailwindcss()] as PluginOption[],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
    },
  },
  server: {
    proxy: {
      '/api': {
        target: 'http://localhost:5172',
        changeOrigin: true,
      },
      '/auth': {
        target: 'http://localhost:5172',
        changeOrigin: true,
      },
    },
  },
  build: {
    // 输出目录指向后端静态资源根目录
    outDir: fileURLToPath(new URL('../backEnd/static', import.meta.url)),
    // 自定义文件命名与目录结构：JS -> /js，CSS -> /css
    rollupOptions: {
      output: {
        entryFileNames: 'js/[name]-[hash].js',
        chunkFileNames: 'js/[name]-[hash].js',
        /**
         * 按类型为静态资源分配输出目录
         * - CSS 文件输出到 /css
         * - 其它静态资源保持在 /assets
         * @param assetInfo 资源信息对象
         * @returns 资源输出路径模板
         */
        assetFileNames: (assetInfo) => {
          const name = assetInfo.name ?? ''
          if (name.toLowerCase().endsWith('.css')) {
            return 'css/[name]-[hash][extname]'
          }
          return 'assets/[name]-[hash][extname]'
        },
      },
    },
  },
})
