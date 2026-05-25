import { ref, watch, type Ref } from 'vue'

/**
 * 防抖 Composable — 对来源 Ref 的变化进行防抖处理
 * @param source 来源响应式值
 * @param delay 防抖延迟（ms），默认 300
 * @returns 防抖后的响应式值
 */
export function useDebounce<T>(source: Ref<T>, delay: number = 300): Ref<T> {
  const debounced = ref(source.value) as Ref<T>
  let timer: ReturnType<typeof setTimeout> | null = null

  watch(
    source,
    (val) => {
      if (timer !== null) {
        clearTimeout(timer)
      }
      timer = setTimeout(() => {
        debounced.value = val
        timer = null
      }, delay)
    },
    { immediate: false },
  )

  return debounced
}
