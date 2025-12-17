<template>
  <div class="flex min-h-screen w-full">
    <ResizableSidebar />
    <main class="flex-1 overflow-auto p-2">
      <p class="text-2xl">APP.vue</p>
      <Button label="test" icon="pi pi-check" />
      <DatePicker v-model="dateData" show-button-bar />
    </main>
  </div>
</template>

<script setup lang="ts">
import { Button, DatePicker } from 'primevue'
import { ref } from 'vue'
import ResizableSidebar from '@/components/ResizableSidebar.vue'

const dateData = ref<Date>(new Date())
</script>

<style scoped></style>
