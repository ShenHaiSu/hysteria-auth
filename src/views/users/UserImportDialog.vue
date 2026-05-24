<template>
  <Dialog
    v-model:visible="dialogVisible"
    :header="t('users.import.title')"
    class="w-full max-w-4xl"
    :modal="true"
    :pt="{ root: { class: 'rounded-lg' }, content: { class: 'bg-[var(--bg-elevated)]' } }"
  >
    <!-- 步骤 1：上传文件 -->
    <template v-if="currentStep === 'upload'">
      <div class="space-y-4">
        <div class="flex items-center gap-3">
          <p class="text-sm text-[var(--text-secondary)]">
            {{ t('users.import.uploadHint') }}
          </p>
          <a
            href="#"
            class="text-sm text-brand-500 hover:text-brand-600"
            @click.prevent="downloadTemplate"
          >
            <i class="pi pi-download mr-1" />
            {{ t('users.import.downloadTemplate') }}
          </a>
        </div>

        <!-- 文件拖拽上传区域 -->
        <div
          class="border-2 border-dashed border-[var(--border-default)] rounded-lg p-8 text-center transition-colors duration-100 hover:border-brand-500"
          :class="{ 'border-brand-500 bg-brand-50 dark:bg-brand-900': isDragover }"
          @dragover.prevent="isDragover = true"
          @dragleave.prevent="isDragover = false"
          @drop.prevent="handleFileDrop"
        >
          <input
            ref="fileInputRef"
            type="file"
            accept=".xlsx,.xls"
            class="hidden"
            @change="handleFileSelect"
          />
          <i class="pi pi-file-excel text-4xl text-[var(--text-muted)] mb-3 block" />
          <p class="text-sm text-[var(--text-secondary)] mb-2">
            {{ selectedFile ? selectedFile.name : t('users.import.dropHint') }}
          </p>
          <p v-if="!selectedFile" class="text-xs text-[var(--text-muted)] mb-3">
            {{ t('users.import.fileFormatHint') }}
          </p>
          <Button
            v-if="!selectedFile"
            :label="t('users.import.selectFile')"
            icon="pi pi-folder-open"
            severity="secondary"
            size="small"
            @click="fileInputRef?.click()"
          />
          <Button
            v-else
            :label="t('users.import.clearFile')"
            icon="pi pi-times"
            severity="secondary"
            size="small"
            @click="clearFile"
          />
        </div>

        <!-- 解析校验结果摘要 -->
        <div v-if="parseResult" class="bg-[var(--bg-secondary)] rounded-md p-4 space-y-2">
          <p class="text-sm font-medium text-[var(--text-primary)]">
            {{ t('users.import.parseResult') }}
          </p>
          <div class="flex items-center gap-4 text-sm">
            <span class="text-[var(--status-active)]">
              <i class="pi pi-check-circle mr-1" />
              {{ t('users.import.validCount', { count: parseResult.valid.length }) }}
            </span>
            <span v-if="parseResult.invalid.length > 0" class="text-[var(--status-error)]">
              <i class="pi pi-times-circle mr-1" />
              {{ t('users.import.invalidCount', { count: parseResult.invalid.length }) }}
            </span>
          </div>
        </div>
      </div>
    </template>

    <!-- 步骤 2：预览确认 -->
    <template v-if="currentStep === 'preview' && parseResult">
      <div class="space-y-4">
        <p class="text-sm text-[var(--text-secondary)]">
          {{ t('users.import.previewHint', { total: parseResult.valid.length }) }}
        </p>

        <div class="overflow-x-auto max-h-80">
          <table class="w-full text-sm">
            <thead>
              <tr class="border-b border-[var(--border-light)] sticky top-0 bg-[var(--bg-elevated)]">
                <th class="text-left py-2 px-3 text-xs text-[var(--text-muted)] font-medium">#</th>
                <th class="text-left py-2 px-3 text-xs text-[var(--text-muted)] font-medium">
                  {{ t('users.table.columns.username') }}
                </th>
                <th class="text-left py-2 px-3 text-xs text-[var(--text-muted)] font-medium">
                  {{ t('users.table.columns.email') }}
                </th>
                <th class="text-left py-2 px-3 text-xs text-[var(--text-muted)] font-medium">
                  {{ t('users.table.columns.totalTraffic') }}
                </th>
                <th class="text-left py-2 px-3 text-xs text-[var(--text-muted)] font-medium">
                  {{ t('users.table.columns.remark') }}
                </th>
                <th class="text-left py-2 px-3 text-xs text-[var(--text-muted)] font-medium">
                  {{ t('users.import.status') }}
                </th>
              </tr>
            </thead>
            <tbody>
              <tr
                v-for="(item, idx) in parseResult.valid"
                :key="idx"
                class="border-b border-[var(--border-light)]"
              >
                <td class="py-2 px-3 text-[var(--text-secondary)]">{{ idx + 1 }}</td>
                <td class="py-2 px-3 text-[var(--text-primary)] font-medium">{{ item.username }}</td>
                <td class="py-2 px-3 text-[var(--text-secondary)]">{{ item.email ?? '-' }}</td>
                <td class="py-2 px-3 text-[var(--text-secondary)]">
                  {{ item.totalTrafficBytes ? formatFileSize(item.totalTrafficBytes) : '-' }}
                </td>
                <td class="py-2 px-3 text-[var(--text-secondary)]">{{ item.remark ?? '-' }}</td>
                <td class="py-2 px-3">
                  <span class="text-xs text-[var(--status-active)] bg-[var(--status-active-bg)] px-2 py-0.5 rounded-sm">
                    ✓
                  </span>
                </td>
              </tr>
              <!-- 错误行 -->
              <template v-if="parseResult.invalid.length > 0">
                <tr>
                  <td colspan="6" class="py-2 px-3">
                    <span class="text-xs font-medium text-[var(--status-error)]">
                      {{ t('users.import.invalidRows') }}
                    </span>
                  </td>
                </tr>
                <tr
                  v-for="(item, idx) in parseResult.invalid"
                  :key="'e' + idx"
                  class="border-b border-[var(--border-light)] bg-[var(--status-error-bg)]"
                >
                  <td class="py-2 px-3 text-[var(--text-secondary)]">{{ item.row }}</td>
                  <td class="py-2 px-3 text-[var(--text-primary)]">{{ item.username ?? '-' }}</td>
                  <td class="py-2 px-3 text-[var(--text-secondary)]">{{ item.email ?? '-' }}</td>
                  <td class="py-2 px-3 text-[var(--text-secondary)]" colspan="2">
                    <span class="text-xs text-[var(--status-error)]">{{ item.reason }}</span>
                  </td>
                  <td class="py-2 px-3">
                    <span class="text-xs text-[var(--status-error)] bg-[var(--status-error-bg)] px-2 py-0.5 rounded-sm">
                      ✗
                    </span>
                  </td>
                </tr>
              </template>
            </tbody>
          </table>
        </div>
      </div>
    </template>

    <!-- 步骤 3：导入进度 -->
    <template v-if="currentStep === 'importing'">
      <div class="flex flex-col items-center py-8 space-y-4">
        <i class="pi pi-spinner animate-spin text-3xl text-brand-500" />
        <p class="text-sm text-[var(--text-secondary)]">
          {{ t('users.import.importing', { current: importProgress.current, total: importProgress.total }) }}
        </p>
        <div class="w-full bg-[var(--bg-secondary)] rounded-full h-2">
          <div
            class="bg-brand-500 h-2 rounded-full transition-all duration-100"
            :style="{ width: importProgress.total > 0 ? (importProgress.current / importProgress.total * 100) + '%' : '0%' }"
          />
        </div>
      </div>
    </template>

    <!-- 步骤 4：导入结果 -->
    <template v-if="currentStep === 'result'">
      <div class="flex flex-col items-center py-6 space-y-4">
        <i
          v-if="importResult.failed === 0"
          class="pi pi-check-circle text-4xl text-[var(--status-active)]"
        />
        <i
          v-else-if="importResult.success > 0"
          class="pi pi-exclamation-triangle text-4xl text-[var(--status-warning)]"
        />
        <i
          v-else
          class="pi pi-times-circle text-4xl text-[var(--status-error)]"
        />
        <div class="text-center">
          <p class="text-lg font-semibold text-[var(--text-primary)]">
            {{ t('users.import.resultTitle') }}
          </p>
          <p class="text-sm text-[var(--text-secondary)] mt-1">
            {{ t('users.import.resultDetail', { success: importResult.success, failed: importResult.failed }) }}
          </p>
        </div>
      </div>
    </template>

    <template #footer>
      <div class="flex items-center justify-between w-full">
        <Button
          v-if="currentStep === 'preview'"
          :label="t('common.actions.back')"
          icon="pi pi-arrow-left"
          severity="secondary"
          @click="currentStep = 'upload'"
        />
        <span v-else />

        <div class="flex gap-2">
          <Button
            v-if="currentStep !== 'importing' && currentStep !== 'result'"
            :label="t('common.actions.cancel')"
            severity="secondary"
            @click="dialogVisible = false"
          />
          <Button
            v-if="currentStep === 'upload' && parseResult && parseResult.valid.length > 0"
            :label="t('users.import.startImport')"
            @click="goToPreview"
          />
          <Button
            v-if="currentStep === 'upload' && !parseResult"
            :label="t('users.import.parse')"
            :disabled="!selectedFile"
            @click="handleParse"
          />
          <Button
            v-if="currentStep === 'preview'"
            :label="t('users.import.startImport')"
            :disabled="!parseResult || parseResult.valid.length === 0"
            @click="handleImport"
          />
          <Button
            v-if="currentStep === 'result'"
            :label="t('common.actions.close')"
            @click="handleClose"
          />
        </div>
      </div>
    </template>
  </Dialog>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useToast } from '@/composables/useToast'
import { useUsersStore } from '@/stores/users.store'
import { parseExcelFile, downloadExcel, generateExportFilename } from '@/utils/export-xlsx'
import { formatFileSize } from '@/utils/format'

const { t } = useI18n()
const toast = useToast()
const usersStore = useUsersStore()

// -- Props --
const props = defineProps<{
  visible: boolean
}>()

const emit = defineEmits<{
  'update:visible': [value: boolean]
  'imported': []
}>()

const dialogVisible = computed({
  get: () => props.visible,
  set: (val) => emit('update:visible', val),
})

// -- State --
type ImportStep = 'upload' | 'preview' | 'importing' | 'result'
const currentStep = ref<ImportStep>('upload')

const fileInputRef = ref<HTMLInputElement>()
const selectedFile = ref<File | null>(null)
const isDragover = ref(false)

interface ValidRow {
  username: string
  password: string
  email?: string
  totalTrafficBytes?: number
  remark?: string
  allowedNodes?: string
}

interface InvalidRow {
  row: number
  username?: string
  email?: string
  reason: string
}

interface ParseResult {
  valid: ValidRow[]
  invalid: InvalidRow[]
}

const parseResult = ref<ParseResult | null>(null)

const importProgress = ref({ current: 0, total: 0 })
const importResult = ref({ success: 0, failed: 0 })

// -- File handling --
function handleFileSelect(e: Event) {
  const input = e.target as HTMLInputElement
  if (input.files && input.files[0]) {
    selectedFile.value = input.files[0]
    parseResult.value = null
    currentStep.value = 'upload'
  }
}

function handleFileDrop(e: DragEvent) {
  isDragover.value = false
  const file = e.dataTransfer?.files?.[0]
  if (file) {
    selectedFile.value = file
    parseResult.value = null
    currentStep.value = 'upload'
  }
}

function clearFile() {
  selectedFile.value = null
  parseResult.value = null
  if (fileInputRef.value) {
    fileInputRef.value.value = ''
  }
  currentStep.value = 'upload'
}

// -- Parse --
async function handleParse() {
  if (!selectedFile.value) return

  try {
    const rawData = await parseExcelFile(selectedFile.value)
    const result: ParseResult = { valid: [], invalid: [] }

    for (let i = 0; i < rawData.length; i++) {
      const row = rawData[i]!
      const rowNum = i + 2 // +2 because header is row 1

      const username = String(row['用户名'] ?? row['username'] ?? '').trim()
      const password = String(row['密码'] ?? row['password'] ?? '').trim()
      const email = String(row['邮箱'] ?? row['email'] ?? '').trim() || undefined

      // 校验必填字段
      if (!username) {
        result.invalid.push({ row: rowNum, username, reason: t('users.import.errors.missingUsername') })
        continue
      }
      if (!password) {
        result.invalid.push({ row: rowNum, username, reason: t('users.import.errors.missingPassword') })
        continue
      }

      // 流量配额解析
      let totalTrafficBytes: number | undefined
      const rawTraffic = row['总流量配额'] ?? row['totalTrafficBytes'] ?? row['totalTraffic']
      if (rawTraffic !== undefined && rawTraffic !== null && String(rawTraffic).trim() !== '') {
        const parsed = parseInt(String(rawTraffic), 10)
        if (isNaN(parsed) || parsed < 0) {
          result.invalid.push({ row: rowNum, username, reason: t('users.import.errors.invalidTraffic') })
          continue
        }
        totalTrafficBytes = parsed
      }

      const remark = String(row['备注'] ?? row['remark'] ?? '').trim() || undefined
      const allowedNodes = String(row['允许节点'] ?? row['allowedNodes'] ?? '').trim() || undefined

      result.valid.push({
        username,
        password,
        email,
        totalTrafficBytes,
        remark,
        allowedNodes,
      })
    }

    parseResult.value = result

    if (result.valid.length === 0 && result.invalid.length > 0) {
      toast.error(t('users.import.errors.allInvalid'))
    }
  } catch {
    toast.error(t('users.import.errors.parseFailed'))
    clearFile()
  }
}

function goToPreview() {
  if (parseResult.value && parseResult.value.valid.length > 0) {
    currentStep.value = 'preview'
  }
}

// -- Import --
async function handleImport() {
  if (!parseResult.value || parseResult.value.valid.length === 0) return

  currentStep.value = 'importing'
  const items = parseResult.value.valid
  importProgress.value = { current: 0, total: items.length }
  importResult.value = { success: 0, failed: 0 }

  // 并发池，每批 5 个
  const BATCH_SIZE = 5
  for (let i = 0; i < items.length; i += BATCH_SIZE) {
    const batch = items.slice(i, i + BATCH_SIZE)
    const results = await Promise.allSettled(
      batch.map((item) =>
        usersStore.createUser({
          username: item.username,
          password: item.password,
          email: item.email,
          totalTrafficBytes: item.totalTrafficBytes,
          remark: item.remark,
          allowedNodes: item.allowedNodes
            ? item.allowedNodes.split(',').map((s) => s.trim()).filter(Boolean)
            : undefined,
        }),
      ),
    )

    for (const r of results) {
      importProgress.value.current++
      if (r.status === 'fulfilled') {
        importResult.value.success++
      } else {
        importResult.value.failed++
      }
    }
  }

  currentStep.value = 'result'
  if (importResult.value.success > 0) {
    emit('imported')
  }
}

// -- Template download --
function downloadTemplate() {
  const data = [
    { username: 'example', password: 'password123', email: 'user@example.com', totalTrafficBytes: '10737418240', remark: '示例用户', allowedNodes: 'node1,node2' },
  ]
  downloadExcel(
    data,
    [
      { header: '用户名', key: 'username' },
      { header: '密码', key: 'password' },
      { header: '邮箱', key: 'email' },
      { header: '总流量配额', key: 'totalTrafficBytes' },
      { header: '备注', key: 'remark' },
      { header: '允许节点', key: 'allowedNodes' },
    ],
    generateExportFilename(t('users.import.templatePrefix')),
    'Template',
  )
}

// -- Close / Reset --
function handleClose() {
  dialogVisible.value = false
  resetState()
}

function resetState() {
  currentStep.value = 'upload'
  selectedFile.value = null
  parseResult.value = null
  if (fileInputRef.value) fileInputRef.value.value = ''
}
</script>
