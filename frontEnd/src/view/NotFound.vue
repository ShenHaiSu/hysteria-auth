<template>
  <div class="relative min-h-screen flex items-center justify-center overflow-hidden bg-surface-50 dark:bg-surface-950 px-4 py-12">
    <!-- 背景动效装饰 (Background Decoration) -->
    <div class="absolute inset-0 z-0">
      <div class="absolute top-[-10%] left-[-10%] w-[50%] h-[50%] bg-primary/10 rounded-full blur-[120px] animate-pulse"></div>
      <div class="absolute bottom-[-10%] right-[-10%] w-[50%] h-[50%] bg-primary/5 rounded-full blur-[120px] animate-pulse [animation-delay:2s]"></div>
      <div class="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 w-full h-full opacity-[0.03] pointer-events-none bg-[radial-gradient(#000_1px,transparent_1px)] bg-size-[30px_30px] dark:bg-[radial-gradient(#fff_1px,transparent_1px)]"></div>
    </div>

    <!-- 主容器 (Main Container) -->
    <div class="relative z-10 w-full max-w-6xl">
      <div class="glass-card rounded-[2.5rem] border border-surface-200/50 dark:border-surface-800/50 backdrop-blur-2xl bg-white/40 dark:bg-black/40 shadow-2xl overflow-hidden transition-all duration-700">
        
        <div class="flex flex-col lg:flex-row divide-y lg:divide-y-0 lg:divide-x divide-surface-200/50 dark:divide-surface-800/50">
          
          <!-- 左侧/上方：核心信息区 (Left/Top: Core Info) -->
          <div class="flex-1 p-8 md:p-16 flex flex-col justify-center items-center text-center">
            <div class="relative mb-12 group">
              <h1 class="glitch text-[10rem] md:text-[14rem] font-black leading-none select-none" data-text="404">
                <span class="bg-clip-text text-transparent bg-linear-to-b from-primary to-primary-700 dark:from-primary-400 dark:to-primary">404</span>
              </h1>
              <div class="absolute -bottom-4 left-1/2 -translate-x-1/2 w-32 h-1.5 bg-primary rounded-full animate-bounce"></div>
            </div>

            <div class="space-y-6 mb-12">
              <h2 class="text-4xl md:text-5xl font-extrabold text-surface-900 dark:text-surface-0 tracking-tight">
                迷失在虚无中
              </h2>
              <p class="text-xl md:text-2xl text-surface-600 dark:text-surface-400 max-w-lg mx-auto leading-relaxed">
                路径 <code class="px-3 py-1 bg-primary/10 text-primary rounded-lg font-mono text-base">{{ route.path }}</code> <br>已被维度抹除。
              </p>
            </div>

            <div class="flex flex-col sm:flex-row items-center justify-center gap-6 w-full">
              <Button 
                label="回到现实 (首页)" 
                icon="pi pi-home" 
                @click="goHome" 
                size="large"
                class="custom-btn w-full sm:w-auto rounded-2xl! px-10! py-4! text-lg"
              />
              <Button 
                label="原路返回" 
                icon="pi pi-arrow-left" 
                @click="goBack" 
                variant="outlined"
                severity="secondary"
                size="large"
                class="custom-btn w-full sm:w-auto rounded-2xl! px-10! py-4! text-lg"
              />
            </div>
          </div>

          <!-- 右侧/下方：增强技术细节区 (Right/Bottom: Enhanced Tech Details) -->
          <div class="flex-1 p-8 md:p-16 bg-surface-100/30 dark:bg-surface-900/30 backdrop-blur-sm">
            <div class="flex items-center gap-3 mb-8">
              <div class="w-3 h-3 rounded-full bg-primary animate-ping"></div>
              <h3 class="text-xl font-bold text-surface-800 dark:text-surface-200 uppercase tracking-widest">System Diagnostics</h3>
            </div>

            <div class="grid grid-cols-1 gap-y-4 font-mono text-sm md:text-base">
              <div class="tech-item group">
                <span class="tech-label">TIMESTAMP</span>
                <span class="tech-value text-primary font-bold">{{ currentTime }}</span>
              </div>
              <div class="tech-item group">
                <span class="tech-label">REQUEST_URL</span>
                <span class="tech-value break-all">{{ windowOrigin }}{{ route.fullPath }}</span>
              </div>
              <div class="tech-item group">
                <span class="tech-label">ROUTE_PATH</span>
                <span class="tech-value">{{ route.path }}</span>
              </div>
              <div class="tech-item group" v-if="Object.keys(route.query).length > 0">
                <span class="tech-label">QUERY_PARAMS</span>
                <span class="tech-value">{{ JSON.stringify(route.query) }}</span>
              </div>
              <div class="tech-item group">
                <span class="tech-label">PLATFORM</span>
                <span class="tech-value">{{ platform }}</span>
              </div>
              <div class="tech-item group">
                <span class="tech-label">LANGUAGE</span>
                <span class="tech-value">{{ language }}</span>
              </div>
              <div class="tech-item group">
                <span class="tech-label">SCREEN_RES</span>
                <span class="tech-value">{{ screenRes }}</span>
              </div>
              <div class="tech-item group">
                <span class="tech-label">VIEWPORT</span>
                <span class="tech-value">{{ viewportSize }}</span>
              </div>
              <div class="tech-item group mt-4">
                <span class="tech-label block mb-2">USER_AGENT</span>
                <div class="tech-value p-4 bg-surface-200/50 dark:bg-surface-800/50 rounded-xl leading-relaxed opacity-70 group-hover:opacity-100 transition-opacity">
                  {{ userAgent }}
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
      
      <!-- 底部版权 (Footer) -->
      <div class="flex flex-col md:flex-row justify-between items-center mt-12 px-6 gap-4 text-surface-400 dark:text-surface-600 text-sm font-mono">
        <p>你好,世界.</p>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted, computed } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import Button from 'primevue/button'

const router = useRouter()
const route = useRoute()

// #region 响应式状态 (Reactive State)
const currentTime = ref(new Date().toISOString())
const userAgent = navigator.userAgent
const platform = (navigator as any).platform || 'Unknown'
const language = navigator.language
const windowOrigin = window.location.origin
const viewportSize = ref(`${window.innerWidth} x ${window.innerHeight}`)

let timer: number | null = null
// #endregion

// #region 计算属性 (Computed)
const screenRes = computed(() => `${window.screen.width} x ${window.screen.height}`)
// #endregion

// #region 逻辑处理 (Logic)
/**
 * 跳转回首页
 */
function goHome() {
  router.push('/')
}

/**
 * 返回上一页
 */
function goBack() {
  router.back()
}

/**
 * 更新环境信息
 */
function updateEnvironment() {
  currentTime.value = new Date().toISOString()
  viewportSize.value = `${window.innerWidth} x ${window.innerHeight}`
}

onMounted(() => {
  timer = window.setInterval(updateEnvironment, 1000)
  window.addEventListener('resize', updateEnvironment)
})

onUnmounted(() => {
  if (timer) clearInterval(timer)
  window.removeEventListener('resize', updateEnvironment)
})
// #endregion
</script>

<style scoped>
/* 404 故障艺术效果 (Glitch Effect) */
.glitch {
  position: relative;
  display: inline-block;
}

.glitch::before,
.glitch::after {
  content: attr(data-text);
  position: absolute;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  background-clip: text;
  -webkit-background-clip: text;
  color: transparent;
}

.glitch::before {
  left: 3px;
  text-shadow: -3px 0 #ff00c1;
  clip: rect(44px, 450px, 56px, 0);
  animation: glitch-anim 4s infinite linear alternate-reverse;
}

.glitch::after {
  left: -3px;
  text-shadow: -3px 0 #00fff9, 3px 3px #ff00c1;
  animation: glitch-anim2 0.8s infinite linear alternate-reverse;
}

@keyframes glitch-anim {
  0% { clip: rect(31px, 9999px, 94px, 0); transform: skew(0.85deg); }
  20% { clip: rect(70px, 9999px, 71px, 0); transform: skew(0.58deg); }
  40% { clip: rect(10px, 9999px, 40px, 0); transform: skew(0.2deg); }
  60% { clip: rect(80px, 9999px, 90px, 0); transform: skew(0.9deg); }
  80% { clip: rect(20px, 9999px, 30px, 0); transform: skew(0.4deg); }
  100% { clip: rect(67px, 9999px, 62px, 0); transform: skew(0.1deg); }
}

@keyframes glitch-anim2 {
  0% { clip: rect(65px, 9999px, 100px, 0); transform: skew(0.15deg); }
  23% { clip: rect(25px, 9999px, 30px, 0); transform: skew(0.4deg); }
  50% { clip: rect(80px, 9999px, 85px, 0); transform: skew(0.1deg); }
  75% { clip: rect(10px, 9999px, 15px, 0); transform: skew(0.8deg); }
  100% { clip: rect(10px, 9999px, 85px, 0); transform: skew(0.2deg); }
}

/* 按钮动效优化 (Button Hover Optimization) */
.custom-btn {
  transition: all 0.5s cubic-bezier(0.4, 0, 0.2, 1) !important;
}

.custom-btn:hover {
  transform: translateY(-5px) scale(1.05);
  box-shadow: 0 10px 25px -5px rgba(var(--p-primary-color-rgb), 0.4);
}

/* 技术细节条目样式 (Tech Item Styling) */
.tech-item {
  display: flex;
  flex-direction: column;
  padding-top: 0.75rem;
  padding-bottom: 0.75rem;
  border-bottom: 1px solid rgba(var(--p-surface-200-rgb), 0.3);
}
@media (min-width: 640px) {
  .tech-item {
    flex-direction: row;
    justify-content: space-between;
    align-items: center;
  }
}
.dark .tech-item {
  border-bottom-color: rgba(var(--p-surface-800-rgb), 0.3);
}

.tech-label {
  font-size: 0.75rem;
  color: var(--p-surface-500);
  font-weight: 700;
  letter-spacing: 0.1em;
  margin-bottom: 0.25rem;
}
@media (min-width: 640px) {
  .tech-label {
    margin-bottom: 0;
  }
}

.tech-value {
  color: var(--p-surface-700);
}
.dark .tech-value {
  color: var(--p-surface-300);
}

/* 玻璃拟态卡片 (Glassmorphism) */
.glass-card {
  box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.25);
}
</style>
