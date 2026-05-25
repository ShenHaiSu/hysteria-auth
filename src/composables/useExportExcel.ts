import { useI18n } from 'vue-i18n'
import { useToast } from '@/composables/useToast'
import { downloadExcel, generateExportFilename } from '@/utils/export-xlsx'
import type { ExcelColumn } from '@/utils/export-xlsx'

export function useExportExcel() {
  const toast = useToast()
  const { t } = useI18n()

  /**
   * 将数据导出为 Excel 并触发下载
   */
  function exportToExcel<T extends Record<string, unknown>>(
    data: T[],
    columns: ExcelColumn<T>[],
    filenamePrefix: string,
    sheetName: string = 'Sheet1',
  ) {
    try {
      const filename = generateExportFilename(filenamePrefix)
      downloadExcel(data, columns, filename, sheetName)
      toast.success(t('common.toast.exportSuccess'))
    } catch (err) {
      console.error('Excel export failed:', err)
      toast.error(t('common.toast.exportFailed'))
    }
  }

  return { exportToExcel }
}
