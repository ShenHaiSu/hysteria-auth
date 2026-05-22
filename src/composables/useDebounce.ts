import { ref, type Ref } from 'vue'

export function useDebounce<T>(source: Ref<T>, delay: number = 300): Ref<T> {
  const debounced = ref(source.value) as Ref<T>
  let timer: ReturnType<typeof setTimeout> | null = null

  // Note: In real usage this would watch the source, but for the skeleton
  // we export a simple utility.
  // Full debounce will be implemented in Phase 1.

  return debounced
}
