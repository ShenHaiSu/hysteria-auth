<template>
  <h1>You did it!</h1>
  <p class="text-2xl">
    Visit <a href="https://vuejs.org/" target="_blank" rel="noopener">vuejs.org</a> to read the
    documentation
  </p>
  <Button label="点击事件" @click="sendToast"></Button>
  <Toast group="toast" />
  <ConfirmDialog group="dialog" />
  <ConfirmPopup group="popup" />
</template>

<script setup lang="ts">
import { useToast } from 'primevue'

const toast = useToast()

const sendToast = () => {
  toast.add({
    severity: 'info',
    summary: '咕咕嘎',
    detail: '咕咕嘎嘎',
    group: 'toast',
    life: 1000,
  })
}
</script>

<style scoped></style>
