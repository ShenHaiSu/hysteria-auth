<!-- 节点新增/编辑对话框组件 -->
<template>
  <Dialog
    v-model:visible="visible"
    :header="mode === 'add' ? '新增代理节点' : '编辑代理节点'"
    :modal="true"
    :style="{ width: '50rem' }"
    class="p-fluid"
  >
    <div class="grid grid-cols-1 md:grid-cols-2 gap-4 mt-2">
      <!-- 基本信息 -->
      <div class="flex flex-col gap-2">
        <label for="ip_address" class="font-bold"
          >IP 地址 <span class="text-red-500">*</span></label
        >
        <InputText
          id="ip_address"
          v-model="form.ip_address"
          required
          autofocus
          :class="{ 'p-invalid': !form.ip_address }"
        />
      </div>

      <div class="flex flex-col gap-2">
        <label for="domain" class="font-bold">域名</label>
        <InputText id="domain" v-model="form.domain" placeholder="可选" />
      </div>

      <div class="flex flex-col gap-2">
        <label for="server_group" class="font-bold"
          >节点分组 <span class="text-red-500">*</span></label
        >
        <InputText
          id="server_group"
          v-model="form.server_group"
          required
          :class="{ 'p-invalid': !form.server_group }"
        />
        <small class="text-surface-500">同组节点共享 IDC、混淆口令、租金等属性</small>
      </div>

      <div class="flex flex-col gap-2">
        <label for="salamander" class="font-bold">混淆口令 (Salamander)</label>
        <InputText id="salamander" v-model="form.salamander" />
      </div>

      <div class="flex flex-col gap-2">
        <label for="idc_name" class="font-bold">IDC 供应商</label>
        <InputText id="idc_name" v-model="form.idc_name" />
      </div>

      <div class="flex flex-col gap-2">
        <label for="fee" class="font-bold">租用费用</label>
        <InputNumber id="fee" v-model="form.fee" mode="currency" currency="CNY" locale="zh-CN" />
      </div>

      <div class="flex flex-col gap-2">
        <label for="proxy_port" class="font-bold"
          >代理端口 (Proxy Port) <span class="text-red-500">*</span></label
        >
        <InputText
          id="proxy_port"
          v-model="form.proxy_port"
          placeholder="例如: 8080 或 20000-30000"
          :class="{ 'p-invalid': !validateProxyPort(form.proxy_port) }"
        />
        <small class="text-surface-500">支持单个端口或端口范围 (0-65535)</small>
      </div>

      <div class="flex flex-col gap-2">
        <label for="server_port" class="font-bold">服务端端口 (Server Port)</label>
        <InputNumber
          id="server_port"
          v-model="form.server_port"
          :useGrouping="false"
          placeholder="可选"
        />
      </div>

      <div class="flex flex-col gap-2">
        <label for="rent_ts" class="font-bold">租用时间</label>
        <DatePicker
          id="rent_ts"
          v-model="rentDate"
          showIcon
          showTime
          hourFormat="24"
          class="w-full"
        />
      </div>

      <div class="flex flex-col gap-2">
        <label for="expire_ts" class="font-bold">到期时间</label>
        <DatePicker
          id="expire_ts"
          v-model="expireDate"
          showIcon
          showTime
          hourFormat="24"
          placeholder="0 表示长期有效"
          class="w-full"
        />
      </div>

      <div class="flex flex-col gap-2">
        <label for="note1" class="font-bold">备注 1</label>
        <InputText id="note1" v-model="form.note1" />
      </div>

      <div class="flex flex-col gap-2">
        <label for="note2" class="font-bold">备注 2</label>
        <InputText id="note2" v-model="form.note2" />
      </div>

      <div class="flex flex-col gap-2">
        <label for="note3" class="font-bold">备注 3</label>
        <InputText id="note3" v-model="form.note3" />
      </div>

      <div class="flex flex-col gap-2">
        <label for="note4" class="font-bold">备注 4</label>
        <InputText id="note4" v-model="form.note4" />
      </div>

      <div class="flex flex-col gap-2 col-span-full">
        <label class="font-bold mb-2">启用状态</label>
        <div class="flex items-center gap-4">
          <div class="flex items-center gap-2">
            <RadioButton v-model="form.is_active" :value="1" inputId="active_on" />
            <label for="active_on">启用</label>
          </div>
          <div class="flex items-center gap-2">
            <RadioButton v-model="form.is_active" :value="0" inputId="active_off" />
            <label for="active_off">禁用</label>
          </div>
        </div>
      </div>
    </div>

    <template #footer>
      <Button label="取消" icon="pi pi-times" text @click="onCancel" />
      <Button label="保存" icon="pi pi-check" :loading="loading" @click="onSave" />
    </template>
  </Dialog>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import type { NodeSaveRequest } from '@/composable/node'
import Dialog from 'primevue/dialog'
import InputText from 'primevue/inputtext'
import InputNumber from 'primevue/inputnumber'
import DatePicker from 'primevue/datepicker'
import RadioButton from 'primevue/radiobutton'
import Button from 'primevue/button'
import { useToast } from 'primevue/usetoast'

const props = defineProps<{
  visible: boolean
  mode: 'add' | 'edit'
  form: NodeSaveRequest
  loading: boolean
}>()

const emit = defineEmits(['update:visible', 'save', 'cancel'])

const toast = useToast()

/**
 * 内部可见性绑定
 */
const visible = computed({
  get: () => props.visible,
  set: (val) => emit('update:visible', val),
})

/**
 * 时间转换逻辑：Unix 秒转 Date 对象
 */
const rentDate = ref<Date | null>(null)
const expireDate = ref<Date | null>(null)

// 监听 form 变化同步 Date 对象
watch(
  () => props.form,
  (newVal) => {
    if (newVal.rent_ts) {
      rentDate.value = new Date(newVal.rent_ts * 1000)
    } else {
      rentDate.value = null
    }

    if (newVal.expire_ts && newVal.expire_ts > 0) {
      expireDate.value = new Date(newVal.expire_ts * 1000)
    } else {
      expireDate.value = null
    }
  },
  { immediate: true, deep: true },
)

/**
 * 校验代理端口格式
 * @param val 端口字符串
 * @returns 是否合法
 */
function validateProxyPort(val: string) {
  if (!val) return false
  // 纯数字
  if (/^\d+$/.test(val)) {
    const port = parseInt(val)
    return port >= 0 && port <= 65535
  }
  // 数字-数字
  const rangeMatch = val.match(/^(\d+)-(\d+)$/)
  if (rangeMatch && rangeMatch[1] !== undefined && rangeMatch[2] !== undefined) {
    const start = parseInt(rangeMatch[1])
    const end = parseInt(rangeMatch[2])
    return start >= 0 && start <= 65535 && end >= 0 && end <= 65535 && start <= end
  }
  return false
}

/**
 * 取消操作
 */
function onCancel() {
  emit('cancel')
}

/**
 * 保存操作
 */
function onSave() {
  // 校验
  if (!props.form.ip_address) {
    toast.add({ severity: 'error', summary: '错误', detail: '请输入 IP 地址', life: 3000 })
    return
  }
  if (!props.form.server_group) {
    toast.add({ severity: 'error', summary: '错误', detail: '请输入节点分组', life: 3000 })
    return
  }
  if (!validateProxyPort(props.form.proxy_port)) {
    toast.add({
      severity: 'error',
      summary: '错误',
      detail: '代理端口格式不正确 (应为单个端口或范围，如 8080 或 20000-30000)',
      life: 5000,
    })
    return
  }

  // 同步 Date 到 Unix 秒
  const data = { ...props.form }
  if (rentDate.value) {
    data.rent_ts = Math.floor(rentDate.value.getTime() / 1000)
  }
  if (expireDate.value) {
    data.expire_ts = Math.floor(expireDate.value.getTime() / 1000)
  } else {
    data.expire_ts = 0
  }

  emit('save', data)
}
</script>

<style lang="css" scoped>
:deep(.p-datepicker-input) {
  width: 100%;
}
</style>
