import { ref } from 'vue'
import type { MenuItem } from 'primevue/menuitem'

export const sidebarMenuModel = ref<MenuItem[]>([
  {
    label: '概览',
    icon: 'pi pi-home',
    to: '/',
  },
  {
    label: '用户管理',
    icon: 'pi pi-users',
    to: '/users',
  },
  {
    label: '节点管理',
    icon: 'pi pi-server',
    to: '/nodes',
  },
  {
    label: '报表中心',
    icon: 'pi pi-file',
    to: '/reports',
  },
  {
    label: '设置',
    icon: 'pi pi-cog',
    to: '/settings',
  },
])
