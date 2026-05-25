import { useConfirm as usePrimeConfirm } from 'primevue/useconfirm'

/**
 * PrimeVue ConfirmationService 封装 — 统一调用风格
 */
export function useConfirm() {
  const confirm = usePrimeConfirm()

  function danger(message: string, header?: string): Promise<boolean> {
    return new Promise((resolve) => {
      confirm.require({
        message,
        header: header ?? '确认操作',
        icon: 'pi pi-exclamation-triangle',
        rejectLabel: '取消',
        acceptLabel: '确认',
        rejectClass: 'p-button-secondary',
        acceptClass: 'p-button-danger',
        accept: () => resolve(true),
        reject: () => resolve(false),
      })
    })
  }

  function confirmDelete(message: string, header?: string): Promise<boolean> {
    return danger(message, header ?? '确认删除')
  }

  return { danger, confirmDelete }
}
