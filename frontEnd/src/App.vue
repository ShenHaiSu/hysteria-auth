<template>
  <div class="p-2">
    <p class="text-2xl" >APP.vue</p>
    <Button label="test" icon="pi pi-check"></Button>
    <DatePicker v-model="dateData" show-button-bar />
  </div>
</template>

<script setup lang="ts">
import { Button, DatePicker } from 'primevue'
import { ref } from 'vue'

const dateData = ref<Date>(new Date())
</script>

<style scoped></style>
