<!-- 用户编辑/新增对话框 -->
<template>
  <Dialog
    v-model:visible="visible"
    :header="isEdit ? '编辑用户' : '新增用户'"
    :modal="true"
    class="p-fluid w-full max-w-md"
    @hide="onHide"
  >
    <div class="flex flex-col gap-4 mt-2">
      <div class="field">
        <label for="name" class="font-bold block mb-2">姓名</label>
        <InputText
          id="name"
          v-model.trim="formData.name"
          required="true"
          autofocus
          :class="{ 'p-invalid': submitted && !formData.name }"
          placeholder="请输入姓名"
        />
        <small class="p-error block" v-if="submitted && !formData.name"> 姓名是必填项 </small>
      </div>

      <div class="field">
        <label for="email" class="font-bold block mb-2">邮箱</label>
        <InputText
          id="email"
          v-model.trim="formData.email"
          required="true"
          :class="{ 'p-invalid': submitted && !isValidEmail(formData.email) }"
          placeholder="请输入邮箱"
        />
        <small class="p-error block" v-if="submitted && !formData.email"> 邮箱是必填项 </small>
        <small class="p-error block" v-else-if="submitted && !isValidEmail(formData.email)">
          请输入有效的邮箱格式
        </small>
      </div>
    </div>

    <template #footer>
      <Button label="取消" icon="pi pi-times" text @click="hideDialog" />
      <Button label="保存" icon="pi pi-check" :loading="saving" @click="saveUser" />
    </template>
  </Dialog>
</template>

<script setup lang="ts">
import { ref, watch, reactive } from 'vue'
import Dialog from 'primevue/dialog'
import InputText from 'primevue/inputtext'
import Button from 'primevue/button'
import type { UserSaveRequest, UserVO } from '@/composable/user'

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

const formData = reactive<UserSaveRequest>({
  name: '',
  email: '',
})
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
  (newUser) => {
    if (newUser) {
      isEdit.value = true
      formData.name = newUser.name
      formData.email = newUser.email
    } else {
      isEdit.value = false
      formData.name = ''
      formData.email = ''
    }
    submitted.value = false
  },
  { immediate: true },
)
// #endregion

// #region 逻辑处理
/**
 * 校验邮箱格式
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
 * 弹窗隐藏后的清理
 */
const onHide = () => {
  submitted.value = false
}

/**
 * 保存用户信息
 */
const saveUser = () => {
  submitted.value = true

  if (formData.name && isValidEmail(formData.email)) {
    emit('save', { ...formData })
  }
}
// #endregion
</script>
