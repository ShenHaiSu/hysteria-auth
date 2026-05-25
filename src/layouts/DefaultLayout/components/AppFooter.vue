<script setup lang="ts">
const currentYear = new Date().getFullYear()
</script>

<template>
  <footer
    class="h-10 border-t border-[var(--border-light)] flex items-center justify-center text-xs text-[var(--text-muted)] bg-[var(--bg-elevated)] shrink-0"
  >
    <span>Hysteria Auth &copy; {{ currentYear }}</span>
    <span class="mx-2 text-[var(--divider)]">|</span>
    <span>v1.0.0</span>
  </footer>
</template>
