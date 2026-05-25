<template>
  <Dialog
    :visible="visible"
    :header="t('nodes.config.title')"
    :modal="true"
    :closable="true"
    :draggable="false"
    class="w-full max-w-3xl"
    @update:visible="$emit('update:visible', $event)"
  >
    <!-- Tab 切换 -->
    <Tabs v-model:value="activeTab" :scrollable="true">
      <TabList>
        <Tab value="listener">{{ t('nodes.config.tab.listener') }}</Tab>
        <Tab value="obfs">{{ t('nodes.config.tab.obfs') }}</Tab>
        <Tab value="bandwidth">{{ t('nodes.config.tab.bandwidth') }}</Tab>
        <Tab value="udpSniff">{{ t('nodes.config.tab.udpSniff') }}</Tab>
        <Tab value="masquerade">{{ t('nodes.config.tab.masquerade') }}</Tab>
        <Tab value="dnsOps">{{ t('nodes.config.tab.dnsOps') }}</Tab>
      </TabList>

      <!-- Tab：监听与端口 -->
      <TabPanel value="listener">
        <div class="space-y-4">
          <div class="flex items-center justify-between">
            <h4 class="text-sm font-semibold text-[var(--text-primary)]">
              {{ t('nodes.config.tab.listener') }}
            </h4>
            <Button
              :label="t('nodes.config.resetTab')"
              icon="pi pi-refresh"
              severity="secondary"
              text
              size="small"
              @click="resetTab('listener')"
            />
          </div>
          <div class="flex items-center gap-3">
            <ToggleSwitch :model-value="form.enablePortHopping" @update:model-value="form.enablePortHopping = $event" />
            <label class="text-sm text-[var(--text-primary)]">
              {{ t('nodes.config.fields.enablePortHopping.label') }}
            </label>
          </div>

          <!-- 端口跳跃范围 (仅在启用时显示) -->
          <div v-if="form.enablePortHopping" class="grid grid-cols-1 md:grid-cols-2 gap-4 pl-6 border-l-2 border-[var(--brand-300)]">
            <div>
              <label class="block text-sm font-medium text-[var(--text-primary)] mb-1">
                {{ t('nodes.config.fields.portHopRangeStart.label') }}
                <span class="text-xs text-[var(--text-muted)] ml-1">(61000)</span>
              </label>
              <InputNumber v-model="form.portHopRangeStart" class="w-full" :min="1024" :max="65535" :use-grouping="false" />
              <small v-if="fieldErrors.portHopRangeStart" class="text-[var(--status-error)]">
                {{ fieldErrors.portHopRangeStart }}
              </small>
            </div>
            <div>
              <label class="block text-sm font-medium text-[var(--text-primary)] mb-1">
                {{ t('nodes.config.fields.portHopRangeEnd.label') }}
                <span class="text-xs text-[var(--text-muted)] ml-1">(63000)</span>
              </label>
              <InputNumber v-model="form.portHopRangeEnd" class="w-full" :min="1024" :max="65535" :use-grouping="false" />
              <small v-if="fieldErrors.portHopRangeEnd" class="text-[var(--status-error)]">
                {{ fieldErrors.portHopRangeEnd }}
              </small>
            </div>
          </div>
        </div>
      </TabPanel>

      <!-- Tab：混淆与拥塞控制 -->
      <TabPanel value="obfs">
        <div class="space-y-4">
          <div class="flex items-center justify-between">
            <h4 class="text-sm font-semibold text-[var(--text-primary)]">
              {{ t('nodes.config.tab.obfs') }}
            </h4>
            <Button
              :label="t('nodes.config.resetTab')"
              icon="pi pi-refresh"
              severity="secondary"
              text
              size="small"
              @click="resetTab('obfs')"
            />
          </div>
          <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
            <!-- 混淆类型 -->
            <div>
              <label class="block text-sm font-medium text-[var(--text-primary)] mb-1">
                {{ t('nodes.config.fields.obfsType.label') }}
              </label>
              <Select
                v-model="form.obfsType"
                :options="obfsTypeOptions"
                option-label="label"
                option-value="value"
                :placeholder="t('common.actions.none')"
                class="w-full"
                clearable
              />
            </div>
            <!-- 混淆密码 -->
            <div v-if="form.obfsType">
              <label class="block text-sm font-medium text-[var(--text-primary)] mb-1">
                {{ t('nodes.config.fields.obfsPassword.label') }}
              </label>
              <InputText v-model="form.obfsPassword" class="w-full" :placeholder="t('common.actions.keepUnchanged')" />
              <small class="text-[var(--text-muted)] text-xs">{{ t('nodes.config.hints.obfsPassword') }}</small>
            </div>
            <!-- 拥塞控制 -->
            <div>
              <label class="block text-sm font-medium text-[var(--text-primary)] mb-1">
                {{ t('nodes.config.fields.congestionControl.label') }}
              </label>
              <Select
                v-model="form.congestionControl"
                :options="congestionControlOptions"
                option-label="label"
                option-value="value"
                :placeholder="t('common.actions.none')"
                class="w-full"
                clearable
              />
            </div>
            <!-- Brutal 带宽 (仅 congestionControl = brutal) -->
            <div v-if="form.congestionControl === 'brutal'">
              <label class="block text-sm font-medium text-[var(--text-primary)] mb-1">
                {{ t('nodes.config.fields.brutalTxBandwidth.label') }} ({{ t('nodes.config.fields.brutalTxBandwidth.unit') }})
              </label>
              <InputNumber v-model="form.brutalTxBandwidth" class="w-full" :min="1" :use-grouping="false" />
              <small v-if="fieldErrors.brutalTxBandwidth" class="text-[var(--status-error)]">
                {{ fieldErrors.brutalTxBandwidth }}
              </small>
            </div>
          </div>
        </div>
      </TabPanel>

      <!-- Tab：带宽与速度测试 -->
      <TabPanel value="bandwidth">
        <div class="space-y-4">
          <div class="flex items-center justify-between">
            <h4 class="text-sm font-semibold text-[var(--text-primary)]">
              {{ t('nodes.config.tab.bandwidth') }}
            </h4>
            <Button
              :label="t('nodes.config.resetTab')"
              icon="pi pi-refresh"
              severity="secondary"
              text
              size="small"
              @click="resetTab('bandwidth')"
            />
          </div>
          <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label class="block text-sm font-medium text-[var(--text-primary)] mb-1">
                {{ t('nodes.config.fields.bandwidthUp.label') }}
              </label>
              <InputText v-model="form.bandwidthUp" class="w-full" :placeholder="t('common.status.unlimited')" />
            </div>
            <div>
              <label class="block text-sm font-medium text-[var(--text-primary)] mb-1">
                {{ t('nodes.config.fields.bandwidthDown.label') }}
              </label>
              <InputText v-model="form.bandwidthDown" class="w-full" :placeholder="t('common.status.unlimited')" />
            </div>
            <div class="flex items-center gap-3">
              <ToggleSwitch :model-value="!!form.ignoreClientBandwidth" @update:model-value="form.ignoreClientBandwidth = $event ? true : false" />
              <label class="text-sm text-[var(--text-primary)]">
                {{ t('nodes.config.fields.ignoreClientBandwidth.label') }}
              </label>
            </div>
            <div class="flex items-center gap-3">
              <ToggleSwitch :model-value="!!form.enableSpeedTest" @update:model-value="form.enableSpeedTest = $event ? true : false" />
              <label class="text-sm text-[var(--text-primary)]">
                {{ t('nodes.config.fields.enableSpeedTest.label') }}
              </label>
            </div>
            <div v-if="form.enableSpeedTest">
              <label class="block text-sm font-medium text-[var(--text-primary)] mb-1">
                {{ t('nodes.config.fields.speedTestPingInterval.label') }} ({{ t('nodes.config.fields.speedTestPingInterval.unit') }})
              </label>
              <InputNumber v-model="form.speedTestPingInterval" class="w-full" :min="1" :use-grouping="false" />
              <small v-if="fieldErrors.speedTestPingInterval" class="text-[var(--status-error)]">
                {{ fieldErrors.speedTestPingInterval }}
              </small>
            </div>
          </div>
        </div>
      </TabPanel>

      <!-- Tab：UDP 与协议嗅探 -->
      <TabPanel value="udpSniff">
        <div class="space-y-4">
          <div class="flex items-center justify-between">
            <h4 class="text-sm font-semibold text-[var(--text-primary)]">
              {{ t('nodes.config.tab.udpSniff') }}
            </h4>
            <Button
              :label="t('nodes.config.resetTab')"
              icon="pi pi-refresh"
              severity="secondary"
              text
              size="small"
              @click="resetTab('udpSniff')"
            />
          </div>
          <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label class="block text-sm font-medium text-[var(--text-primary)] mb-1">
                {{ t('nodes.config.fields.udpIdleTimeout.label') }} ({{ t('nodes.config.fields.udpIdleTimeout.unit') }})
              </label>
              <InputNumber v-model="form.udpIdleTimeout" class="w-full" :min="1" :use-grouping="false" />
            </div>
            <div class="flex items-center gap-3">
              <ToggleSwitch :model-value="!!form.sniffEnabled" @update:model-value="form.sniffEnabled = $event ? true : false" />
              <label class="text-sm text-[var(--text-primary)]">
                {{ t('nodes.config.fields.sniffEnabled.label') }}
              </label>
            </div>
          </div>

          <!-- 协议嗅探参数 (仅在启用时显示) -->
          <div v-if="form.sniffEnabled" class="grid grid-cols-1 md:grid-cols-2 gap-4 pl-6 border-l-2 border-[var(--brand-300)]">
            <div>
              <label class="block text-sm font-medium text-[var(--text-primary)] mb-1">
                {{ t('nodes.config.fields.sniffTimeout.label') }} ({{ t('nodes.config.fields.sniffTimeout.unit') }})
              </label>
              <InputNumber v-model="form.sniffTimeout" class="w-full" :min="1" :use-grouping="false" />
            </div>
            <div class="flex items-center gap-3">
              <ToggleSwitch :model-value="!!form.sniffRespectHttps" @update:model-value="form.sniffRespectHttps = $event ? true : false" />
              <label class="text-sm text-[var(--text-primary)]">
                {{ t('nodes.config.fields.sniffRespectHttps.label') }}
              </label>
            </div>
          </div>
        </div>
      </TabPanel>

      <!-- Tab：伪装 -->
      <TabPanel value="masquerade">
        <div class="space-y-4">
          <div class="flex items-center justify-between">
            <h4 class="text-sm font-semibold text-[var(--text-primary)]">
              {{ t('nodes.config.tab.masquerade') }}
            </h4>
            <Button
              :label="t('nodes.config.resetTab')"
              icon="pi pi-refresh"
              severity="secondary"
              text
              size="small"
              @click="resetTab('masquerade')"
            />
          </div>
          <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label class="block text-sm font-medium text-[var(--text-primary)] mb-1">
                {{ t('nodes.config.fields.masqueradeType.label') }}
              </label>
              <Select
                v-model="form.masqueradeType"
                :options="masqueradeTypeOptions"
                option-label="label"
                option-value="value"
                :placeholder="t('common.actions.none')"
                class="w-full"
                clearable
              />
            </div>
          </div>

          <!-- file 类型 -->
          <div v-if="form.masqueradeType === 'file'" class="pl-6 border-l-2 border-[var(--brand-300)]">
            <div class="grid grid-cols-1 gap-4">
              <div>
                <label class="block text-sm font-medium text-[var(--text-primary)] mb-1">
                  {{ t('nodes.config.fields.masqueradeFile.label') }}
                </label>
                <InputText v-model="form.masqueradeFile" class="w-full" />
              </div>
            </div>
          </div>

          <!-- proxy 类型 -->
          <div v-if="form.masqueradeType === 'proxy'" class="pl-6 border-l-2 border-[var(--brand-300)]">
            <div class="grid grid-cols-1 gap-4">
              <div>
                <label class="block text-sm font-medium text-[var(--text-primary)] mb-1">
                  {{ t('nodes.config.fields.masqueradeProxyUrl.label') }}
                </label>
                <InputText v-model="form.masqueradeProxyUrl" class="w-full" />
              </div>
            </div>
          </div>

          <!-- string 类型 -->
          <div v-if="form.masqueradeType === 'string'" class="pl-6 border-l-2 border-[var(--brand-300)]">
            <div class="grid grid-cols-1 gap-4">
              <div>
                <label class="block text-sm font-medium text-[var(--text-primary)] mb-1">
                  {{ t('nodes.config.fields.masqueradeStringContent.label') }}
                </label>
                <InputText v-model="form.masqueradeStringContent" class="w-full" />
              </div>
              <div>
                <label class="block text-sm font-medium text-[var(--text-primary)] mb-1">
                  {{ t('nodes.config.fields.masqueradeStringHeaders.label') }}
                </label>
                <InputText v-model="form.masqueradeStringHeaders" class="w-full" :placeholder="t('nodes.config.hints.masqueradeHeaders')" />
              </div>
              <div>
                <label class="block text-sm font-medium text-[var(--text-primary)] mb-1">
                  {{ t('nodes.config.fields.masqueradeStringStatusCode.label') }}
                </label>
                <InputNumber v-model="form.masqueradeStringStatusCode" class="w-full" :min="100" :max="599" :use-grouping="false" />
              </div>
            </div>
          </div>
        </div>
      </TabPanel>

      <!-- Tab：DNS 与运营 -->
      <TabPanel value="dnsOps">
        <div class="space-y-4">
          <div class="flex items-center justify-between">
            <h4 class="text-sm font-semibold text-[var(--text-primary)]">
              {{ t('nodes.config.tab.dnsOps') }}
            </h4>
            <Button
              :label="t('nodes.config.resetTab')"
              icon="pi pi-refresh"
              severity="secondary"
              text
              size="small"
              @click="resetTab('dnsOps')"
            />
          </div>

          <!-- DNS 部分 -->
          <div class="border-b border-[var(--border-light)] pb-4 mb-4">
            <h5 class="text-sm font-medium text-[var(--text-secondary)] mb-3">DNS</h5>
            <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <label class="block text-sm font-medium text-[var(--text-primary)] mb-1">
                  {{ t('nodes.config.fields.resolverType.label') }}
                </label>
                <Select
                  v-model="form.resolverType"
                  :options="resolverTypeOptions"
                  option-label="label"
                  option-value="value"
                  :placeholder="t('common.actions.none')"
                  class="w-full"
                  clearable
                />
              </div>

              <!-- 根据 resolverType 显示对应地址字段 -->
              <div v-if="form.resolverType === 'tcp'">
                <label class="block text-sm font-medium text-[var(--text-primary)] mb-1">
                  {{ t('nodes.config.fields.resolverTcpAddr.label') }}
                </label>
                <InputText v-model="form.resolverTcpAddr" class="w-full" />
              </div>
              <div v-if="form.resolverType === 'udp'">
                <label class="block text-sm font-medium text-[var(--text-primary)] mb-1">
                  {{ t('nodes.config.fields.resolverUdpAddr.label') }}
                </label>
                <InputText v-model="form.resolverUdpAddr" class="w-full" />
              </div>
              <div v-if="form.resolverType === 'tls'">
                <label class="block text-sm font-medium text-[var(--text-primary)] mb-1">
                  {{ t('nodes.config.fields.resolverTlsAddr.label') }}
                </label>
                <InputText v-model="form.resolverTlsAddr" class="w-full" />
              </div>
            </div>
          </div>

          <!-- 运营管理部分 -->
          <div>
            <h5 class="text-sm font-medium text-[var(--text-secondary)] mb-3">{{ t('nodes.config.tab.dnsOps') }}</h5>
            <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <label class="block text-sm font-medium text-[var(--text-primary)] mb-1">
                  {{ t('nodes.config.fields.serverCost.label') }} ({{ t('nodes.config.fields.serverCost.unit') }})
                </label>
                <InputNumber v-model="form.serverCost" class="w-full" :min="0" :max="999999" />
                <small v-if="fieldErrors.serverCost" class="text-[var(--status-error)]">
                  {{ fieldErrors.serverCost }}
                </small>
              </div>
              <div>
                <label class="block text-sm font-medium text-[var(--text-primary)] mb-1">
                  {{ t('nodes.config.fields.billingCycle.label') }}
                </label>
                <Select
                  v-model="form.billingCycle"
                  :options="billingCycleOptions"
                  option-label="label"
                  option-value="value"
                  :placeholder="t('common.actions.none')"
                  class="w-full"
                  clearable
                />
              </div>
              <div>
                <label class="block text-sm font-medium text-[var(--text-primary)] mb-1">
                  {{ t('nodes.config.fields.expirationDate.label') }}
                </label>
                <DatePicker v-model="expirationDateModel" class="w-full" date-format="yy-mm-dd" show-icon />
              </div>
              <div>
                <label class="block text-sm font-medium text-[var(--text-primary)] mb-1">
                  {{ t('nodes.config.fields.domainName.label') }}
                </label>
                <InputText v-model="form.domainName" class="w-full" />
              </div>
              <div class="md:col-span-2">
                <label class="block text-sm font-medium text-[var(--text-primary)] mb-1">
                  {{ t('nodes.config.fields.remark.label') }}
                </label>
                <Textarea v-model="form.remark" class="w-full" :auto-resize="true" rows="2" />
              </div>
            </div>
          </div>
        </div>
      </TabPanel>
    </Tabs>

    <template #footer>
      <Button :label="t('common.actions.cancel')" severity="secondary" @click="handleClose" />
      <Button
        :label="t('common.actions.save')"
        :loading="nodesStore.isSubmitting"
        :disabled="!hasChanges"
        @click="handleSubmit"
      />
    </template>
  </Dialog>
</template>

<script setup lang="ts">
import { ref, reactive, computed, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useNodesStore } from '@/stores/nodes.store'
import { useToast } from '@/composables/useToast'
import type { NodeDto, UpdateNodeConfigRequest, ObfsType, CongestionControl, MasqueradeType, ResolverType } from '@/types/node.types'

const props = withDefaults(
  defineProps<{
    visible: boolean
    nodeId: string
    currentConfig: NodeDto
  }>(),
  {},
)

const emit = defineEmits<{
  'update:visible': [value: boolean]
  'config-updated': []
}>()

const { t } = useI18n()
const nodesStore = useNodesStore()
const toast = useToast()

const activeTab = ref<string>('listener')
const fieldErrors = ref<Record<string, string>>({})

// 表单数据类型 (所有值要么是原始类型，要么是 null)
interface FormData {
  enablePortHopping: boolean
  portHopRangeStart: number | null
  portHopRangeEnd: number | null
  obfsType: ObfsType | null
  obfsPassword: string | undefined
  congestionControl: CongestionControl | null
  brutalTxBandwidth: number | null
  bandwidthUp: string | null
  bandwidthDown: string | null
  ignoreClientBandwidth: boolean | null
  enableSpeedTest: boolean | null
  speedTestPingInterval: number | null
  udpIdleTimeout: number | null
  sniffEnabled: boolean | null
  sniffTimeout: number | null
  sniffRespectHttps: boolean | null
  masqueradeType: MasqueradeType | null
  masqueradeFile: string | null
  masqueradeProxyUrl: string | null
  masqueradeStringContent: string | null
  masqueradeStringHeaders: string | null
  masqueradeStringStatusCode: number | null
  resolverType: ResolverType | null
  resolverTcpAddr: string | null
  resolverUdpAddr: string | null
  resolverTlsAddr: string | null
  serverCost: number | null
  billingCycle: string | null
  /** ISO date string or null */
  expirationDate: string | null
  domainName: string | null
  remark: string | null
}

// Form data using Record for flexible key access
const form = reactive({} as Record<string, any>)

// 初始值副本（用于差异计算）
let initialSnapshot: Record<string, any> = {}

// DatePicker 需要 Date 类型，代理 expirationDate
const expirationDateModel = computed({
  get: (): Date | undefined => {
    const raw = form.expirationDate
    return raw ? new Date(raw as string) : undefined
  },
  set: (val: unknown) => {
    form.expirationDate = val ? (val as Date).toISOString() : null
  },
})

// 当对话框打开时，初始化表单
watch(
  () => props.visible,
  (visible) => {
    if (visible) {
      initializeForm()
    }
  },
)

function initializeForm() {
  const cfg = props.currentConfig

  form.enablePortHopping = cfg.enablePortHopping ?? true
  form.portHopRangeStart = cfg.portHopRangeStart ?? 61000
  form.portHopRangeEnd = cfg.portHopRangeEnd ?? 63000
  form.obfsType = cfg.obfsType ?? null
  form.obfsPassword = undefined
  form.congestionControl = cfg.congestionControl ?? null
  form.brutalTxBandwidth = cfg.brutalTxBandwidth ?? null
  form.bandwidthUp = cfg.bandwidthUp ?? null
  form.bandwidthDown = cfg.bandwidthDown ?? null
  form.ignoreClientBandwidth = cfg.ignoreClientBandwidth ?? null
  form.enableSpeedTest = cfg.enableSpeedTest ?? null
  form.speedTestPingInterval = cfg.speedTestPingInterval ?? null
  form.udpIdleTimeout = cfg.udpIdleTimeout ?? 60
  form.sniffEnabled = cfg.sniffEnabled ?? null
  form.sniffTimeout = cfg.sniffTimeout ?? null
  form.sniffRespectHttps = cfg.sniffRespectHttps ?? null
  form.masqueradeType = cfg.masqueradeType ?? null
  form.masqueradeFile = cfg.masqueradeFile ?? null
  form.masqueradeProxyUrl = cfg.masqueradeProxyUrl ?? null
  form.masqueradeStringContent = cfg.masqueradeStringContent ?? null
  form.masqueradeStringHeaders = cfg.masqueradeStringHeaders ?? null
  form.masqueradeStringStatusCode = cfg.masqueradeStringStatusCode ?? null
  form.resolverType = cfg.resolverType ?? null
  form.resolverTcpAddr = cfg.resolverTcpAddr ?? null
  form.resolverUdpAddr = cfg.resolverUdpAddr ?? null
  form.resolverTlsAddr = cfg.resolverTlsAddr ?? null
  form.serverCost = cfg.serverCost ?? null
  form.billingCycle = cfg.billingCycle ?? null
  form.expirationDate = cfg.expirationDate ?? null
  form.domainName = cfg.domainName ?? null
  form.remark = cfg.remark ?? null

  // 保存初始快照
  initialSnapshot = { ...form }
  fieldErrors.value = {}
}

/** 检查是否有变更 */
const hasChanges = computed(() => {
  return Object.keys(form).some((key) => {
    const formVal = form[key]
    const initVal = initialSnapshot[key]
    if (formVal === undefined && initVal === null) return false
    if (formVal === null && initVal === undefined) return false
    if (typeof formVal === 'boolean' && initVal === null) return true
    if (typeof formVal === 'boolean' && initVal === undefined) return true
    return formVal !== initVal
  })
})

/** 计算差异：仅返回实际修改的字段 */
function computeDiff(): UpdateNodeConfigRequest {
  const diff: Record<string, unknown> = {}

  for (const key of Object.keys(form)) {
    const formVal = form[key]
    const initVal = initialSnapshot[key]

    const isSame = formVal === initVal ||
      (formVal === undefined && initVal === null) ||
      (formVal === null && initVal === undefined)

    if (!isSame) {
      if (key === 'obfsPassword' && formVal === '') continue
      const val = formVal === undefined ? null : formVal
      diff[key] = val
    }
  }

  return diff as UpdateNodeConfigRequest
}

// 校验
function validate(): boolean {
  const errors: Record<string, string> = {}

  const portStart = form.portHopRangeStart as number | null
  const portEnd = form.portHopRangeEnd as number | null
  if (portStart !== null && portEnd !== null) {
    if (portStart >= portEnd) {
      errors.portHopRangeStart = t('validation.portHopRange')
    }
  }

  if (form.congestionControl === 'brutal') {
    const bw = form.brutalTxBandwidth as number | null
    if (bw === null || bw <= 0) {
      errors.brutalTxBandwidth = t('validation.required')
    }
  }

  if (form.enableSpeedTest) {
    const interval = form.speedTestPingInterval as number | null
    if (interval !== null && interval <= 0) {
      errors.speedTestPingInterval = t('validation.required')
    }
  }

  const cost = form.serverCost as number | null
  if (cost !== null && cost < 0) {
    errors.serverCost = t('validation.minValue')
  }

  fieldErrors.value = errors
  return Object.keys(errors).length === 0
}

async function handleSubmit() {
  if (!validate()) return
  if (!hasChanges.value) {
    handleClose()
    return
  }

  const diff = computeDiff()

  try {
    await nodesStore.updateNodeConfig(props.nodeId, diff)
    toast.success(t('nodes.toast.updateConfigSuccess'))
    emit('config-updated')
  } catch {
    toast.error(t('nodes.toast.updateConfigFailed'))
  }
}

/** 重置某个 Tab 的所有字段为初始值 */
function resetTab(tab: string) {
  const tabDefaults: Record<string, string[]> = {
    listener: ['enablePortHopping', 'portHopRangeStart', 'portHopRangeEnd'],
    obfs: ['obfsType', 'obfsPassword', 'congestionControl', 'brutalTxBandwidth'],
    bandwidth: ['bandwidthUp', 'bandwidthDown', 'ignoreClientBandwidth', 'enableSpeedTest', 'speedTestPingInterval'],
    udpSniff: ['udpIdleTimeout', 'sniffEnabled', 'sniffTimeout', 'sniffRespectHttps'],
    masquerade: ['masqueradeType', 'masqueradeFile', 'masqueradeProxyUrl', 'masqueradeStringContent', 'masqueradeStringHeaders', 'masqueradeStringStatusCode'],
    dnsOps: ['resolverType', 'resolverTcpAddr', 'resolverUdpAddr', 'resolverTlsAddr', 'serverCost', 'billingCycle', 'expirationDate', 'domainName', 'remark'],
  }

  const fields = tabDefaults[tab]
  if (!fields) return

  for (const field of fields) {
    form[field] = initialSnapshot[field]
  }
}

function handleClose() {
  emit('update:visible', false)
}

// ===== Select 选项 =====

const obfsTypeOptions = computed(() => [
  { label: 'Salamander', value: 'salamander' as ObfsType },
])

const congestionControlOptions = computed(() => [
  { label: 'BBR', value: 'bbr' as CongestionControl },
  { label: 'CUBIC', value: 'cubic' as CongestionControl },
  { label: 'Brutal', value: 'brutal' as CongestionControl },
])

const masqueradeTypeOptions = computed(() => [
  { label: t('nodes.config.masqueradeTypes.file'), value: 'file' as MasqueradeType },
  { label: t('nodes.config.masqueradeTypes.proxy'), value: 'proxy' as MasqueradeType },
  { label: t('nodes.config.masqueradeTypes.string'), value: 'string' as MasqueradeType },
  { label: t('nodes.config.masqueradeTypes.reply'), value: 'reply' as MasqueradeType },
])

const resolverTypeOptions = computed(() => [
  { label: t('nodes.config.resolverTypes.system'), value: 'system' as ResolverType },
  { label: t('nodes.config.resolverTypes.udp'), value: 'udp' as ResolverType },
  { label: t('nodes.config.resolverTypes.tcp'), value: 'tcp' as ResolverType },
  { label: t('nodes.config.resolverTypes.tls'), value: 'tls' as ResolverType },
])

const billingCycleOptions = computed(() => [
  { label: t('nodes.config.billingCycles.monthly'), value: 'monthly' },
  { label: t('nodes.config.billingCycles.quarterly'), value: 'quarterly' },
  { label: t('nodes.config.billingCycles.yearly'), value: 'yearly' },
])
</script>
