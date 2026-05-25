import * as XLSX from 'xlsx'

export interface ExcelColumn<T = Record<string, unknown>> {
  /** 列标题 */
  header: string
  /** 数据字段 key */
  key: keyof T | string
  /** 宽度 (字符数)，默认 15 */
  width?: number
  /** 自定义格式化函数 */
  format?: (value: unknown, row: T) => string | number
}

/**
 * 创建 Excel 工作簿下载（纯函数版本，不依赖 Toast）
 */
export function downloadExcel<T extends Record<string, unknown>>(
  data: T[],
  columns: ExcelColumn<T>[],
  filename: string,
  sheetName: string = 'Sheet1',
) {
  const headers = columns.map((col) => col.header)
  const rows = data.map((row) =>
    columns.map((col) => {
      const raw = (row as Record<string, unknown>)[col.key as string]
      return col.format ? col.format(raw, row) : (raw ?? '')
    }),
  )

  const worksheet = XLSX.utils.aoa_to_sheet([headers, ...rows])
  worksheet['!cols'] = columns.map((col) => ({ wch: col.width ?? 15 }))

  const workbook = XLSX.utils.book_new()
  XLSX.utils.book_append_sheet(workbook, worksheet, sheetName)
  XLSX.writeFile(workbook, `${filename}.xlsx`)
}

/**
 * 从上传的 Excel 文件中解析数据
 * @returns 解析后的行数组，每行为 Record<string, unknown>
 */
export function parseExcelFile(file: File): Promise<Record<string, unknown>[]> {
  return new Promise((resolve, reject) => {
    const reader = new FileReader()
    reader.onload = (e) => {
      try {
        const data = new Uint8Array(e.target!.result as ArrayBuffer)
        const workbook = XLSX.read(data, { type: 'array' })
        const firstSheetName = workbook.SheetNames[0]!
        const firstSheet = workbook.Sheets[firstSheetName]!
        const json = XLSX.utils.sheet_to_json<Record<string, unknown>>(firstSheet)
        resolve(json)
      } catch (err) {
        reject(err)
      }
    }
    reader.onerror = reject
    reader.readAsArrayBuffer(file)
  })
}

/**
 * 生成导出文件名（含日期）
 */
export function generateExportFilename(prefix: string): string {
  const now = new Date()
  const dateStr = `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}-${String(now.getDate()).padStart(2, '0')}`
  return `${prefix}_${dateStr}`
}
