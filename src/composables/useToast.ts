import { useToast as usePrimeToast } from 'primevue/usetoast'

/**
 * PrimeVue ToastService 封装 — 统一调用风格
 */
export function useToast() {
  const toast = usePrimeToast()

  function success(detail: string, summary?: string, life?: number) {
    toast.add({ severity: 'success', summary: summary ?? '', detail, life: life ?? 3000 })
  }

  function error(detail: string, summary?: string, life?: number) {
    toast.add({ severity: 'error', summary: summary ?? '', detail, life: life ?? 5000 })
  }

  function warning(detail: string, summary?: string, life?: number) {
    toast.add({ severity: 'warn', summary: summary ?? '', detail, life: life ?? 4000 })
  }

  function info(detail: string, summary?: string, life?: number) {
    toast.add({ severity: 'info', summary: summary ?? '', detail, life: life ?? 3000 })
  }

  return { success, error, warning, info }
}
