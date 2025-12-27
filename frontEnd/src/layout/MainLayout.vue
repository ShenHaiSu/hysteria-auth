<template>
  <div class="flex min-h-screen w-full">
    <ResizableSidebar />
    <main class="flex-1 overflow-auto p-2">
      <router-view />
    </main>
  </div>
</template>

<script setup lang="ts">
import ResizableSidebar from '@/components/ResizableSidebar.vue'
</script>

<style scoped></style>
