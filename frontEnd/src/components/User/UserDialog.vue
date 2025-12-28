<!-- 用户编辑/新增对话框 -->
<template>
  <Dialog
    v-model:visible="visible"
    :header="isEdit ? '编辑用户' : '新增用户'"
    :modal="true"
    class="p-fluid w-full max-w-2xl"
    @hide="onHide"
  >
    <div class="grid grid-cols-1 md:grid-cols-2 gap-x-8 gap-y-6 mt-4">
      <!-- 左侧：基础信息 -->
      <div class="flex flex-col gap-6">
        <h3 class="text-xl font-semibold border-b pb-2 mb-2 text-primary">基础信息</h3>
        <div class="field">
          <label for="username" class="font-bold block mb-2 text-700">用户名</label>
          <InputText
            id="username"
            v-model.trim="formData.username"
            required="true"
            autofocus
            class="w-full"
            :class="{ 'p-invalid': submitted && !formData.username }"
            placeholder="请输入用户名"
          />
          <small class="p-error block mt-1" v-if="submitted && !formData.username">
            用户名是必填项
          </small>
        </div>

        <div class="field">
          <label for="email" class="font-bold block mb-2 text-700">邮箱</label>
          <InputText
            id="email"
            v-model.trim="formData.email"
            required="true"
            class="w-full"
            :class="{ 'p-invalid': submitted && !isValidEmail(formData.email) }"
            placeholder="请输入邮箱"
          />
          <small class="p-error block mt-1" v-if="submitted && !formData.email">
            邮箱是必填项
          </small>
          <small class="p-error block mt-1" v-else-if="submitted && !isValidEmail(formData.email)">
            请输入有效的邮箱格式
          </small>
        </div>

        <div class="field">
          <label for="password" class="font-bold block mb-2 text-700">登录密码</label>
          <Password
            id="password"
            v-model.trim="plainPassword"
            class="w-full"
            input-class="w-full"
            toggle-mask
            :feedback="false"
            placeholder="留空则不修改/使用默认"
          />
        </div>
      </div>

      <!-- 右侧：代理与权限 -->
      <div class="flex flex-col gap-6">
        <h3 class="text-xl font-semibold border-b pb-2 mb-2 text-primary">代理与设置</h3>
        <div class="field">
          <label for="proxy_password" class="font-bold block mb-2 text-700">代理密码</label>
          <InputGroup>
            <InputText
              id="proxy_password"
              v-model.trim="formData.proxy_password"
              placeholder="留空则使用默认"
            />
            <Button
              icon="pi pi-refresh"
              severity="secondary"
              title="生成随机密码"
              @click="generateRandomProxyPassword"
              class="border"
            />
          </InputGroup>
        </div>

        <div class="field">
          <label for="proxy_expire_ts" class="font-bold block mb-2 text-700">代理过期时间</label>
          <DatePicker
            id="proxy_expire_ts"
            v-model="proxyExpireDate"
            showTime
            hourFormat="24"
            class="w-full"
            placeholder="永久有效"
            showIcon
          />
        </div>

        <div class="field">
          <label class="font-bold block mb-2 text-700">权限角色</label>
          <div class="flex gap-4">
            <div class="flex items-center">
              <RadioButton
                v-model="formData.permission"
                inputId="roleUser"
                name="permission"
                value="user"
              />
              <label for="roleUser" class="ml-2">普通用户</label>
            </div>
            <div class="flex items-center">
              <RadioButton
                v-model="formData.permission"
                inputId="roleAdmin"
                name="permission"
                value="admin"
              />
              <label for="roleAdmin" class="ml-2">管理员</label>
            </div>
          </div>
        </div>

        <div class="field flex items-center gap-3 mt-2">
          <ToggleSwitch
            :modelValue="formData.is_active === 1"
            @update:modelValue="(val: boolean) => (formData.is_active = val ? 1 : 0)"
          />
          <label class="font-bold text-700">激活状态</label>
        </div>
      </div>
    </div>

    <template #footer>
      <div class="flex justify-end gap-2 mt-6">
        <Button label="取消" icon="pi pi-times" text class="p-button-lg" @click="hideDialog" />
        <Button
          label="保存"
          icon="pi pi-check"
          :loading="saving"
          class="p-button-lg px-6"
          @click="saveUser"
        />
      </div>
    </template>
  </Dialog>
</template>

<script setup lang="ts">
import type { UserSaveRequest, UserVO } from '@/composable/user'
import { ref, watch } from 'vue'
import md5 from 'md5'
import Dialog from 'primevue/dialog'
import InputText from 'primevue/inputtext'
import Password from 'primevue/password'
import InputGroup from 'primevue/inputgroup'
import Button from 'primevue/button'
import RadioButton from 'primevue/radiobutton'
import DatePicker from 'primevue/datepicker'
import ToggleSwitch from 'primevue/toggleswitch'

// #region 初始化与事件
const props = defineProps<{
  show: boolean
  user: UserVO | null
  saving: boolean
}>()

const emit = defineEmits<{
  (e: 'update:show', value: boolean): void
  (e: 'save', data: UserSaveRequest): void
}>()

const visible = ref(props.show)
const isEdit = ref(false)
const submitted = ref(false)
/** 用户输入的明文密码 */
const plainPassword = ref('')

const formData = ref<UserSaveRequest>({
  username: '',
  email: '',
  permission: 'user',
  is_active: 1,
  proxy_password: '',
  proxy_expire_ts: 0,
})

const proxyExpireDate = ref<Date | null>(null)
// #endregion

// #region 逻辑处理
/**
 * 重置表单数据为初始状态或当前用户数据
 */
const resetForm = () => {
  plainPassword.value = ''
  if (props.user) {
    isEdit.value = true
    formData.value = {
      username: props.user.username,
      email: props.user.email,
      permission: props.user.permission,
      is_active: props.user.is_active,
      proxy_password: props.user.proxy_password,
      proxy_expire_ts: props.user.proxy_expire_ts,
    }
    proxyExpireDate.value =
      props.user.proxy_expire_ts && props.user.proxy_expire_ts > 0
        ? new Date(props.user.proxy_expire_ts * 1000)
        : null
  } else {
    isEdit.value = false
    formData.value = {
      username: '',
      email: '',
      permission: 'user',
      is_active: 1,
      proxy_password: '',
      proxy_expire_ts: 0,
    }
    proxyExpireDate.value = null
  }
  submitted.value = false
}

/**
 * 校验邮箱格式
 * @param email 待校验的邮箱字符串
 * @returns 是否为有效的邮箱格式
 */
const isValidEmail = (email: string) => {
  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)
}

/**
 * 关闭弹窗
 */
const hideDialog = () => {
  visible.value = false
}

/**
 * 弹窗隐藏后的清理工作
 */
const onHide = () => {
  resetForm()
}

/**
 * 保存用户信息，执行表单校验并触发 save 事件
 */
const saveUser = () => {
  submitted.value = true

  // 同步日期到时间戳
  if (proxyExpireDate.value) {
    formData.value.proxy_expire_ts = Math.floor(proxyExpireDate.value.getTime() / 1000)
  } else {
    formData.value.proxy_expire_ts = 0
  }

  // 处理密码 MD5 加密
  const submitData = { ...formData.value }
  if (plainPassword.value) {
    submitData.login_password_md5 = md5(plainPassword.value)
  }

  if (formData.value.username && isValidEmail(formData.value.email)) {
    emit('save', submitData)
  }
}

/**
 * 生成 20 位随机代理密码（由大小写字母和数字组成）
 */
const generateRandomProxyPassword = () => {
  const chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789'
  let result = ''
  for (let i = 0; i < 20; i++) {
    result += chars.charAt(Math.floor(Math.random() * chars.length))
  }
  formData.value.proxy_password = result
}
// #endregion

// #region 监听器
watch(
  () => props.show,
  (newVal) => {
    visible.value = newVal
  },
)

watch(visible, (newVal) => {
  emit('update:show', newVal)
})

watch(
  () => props.user,
  () => {
    resetForm()
  },
  { immediate: true },
)
// #endregion
</script>

<style lang="css" scoped>
:deep(.p-datepicker-input) {
  width: 100%;
}
</style>
