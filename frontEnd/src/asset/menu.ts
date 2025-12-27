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
    items: [
      { label: '用户列表', icon: 'pi pi-list', to: '/users' },
      { label: '新建用户', icon: 'pi pi-user-plus', to: '/users/new' },
    ],
  },
  {
    label: '设置',
    icon: 'pi pi-cog',
    items: [
      { label: '个人资料', icon: 'pi pi-id-card', to: '/settings/profile' },
      { label: '安全设置', icon: 'pi pi-shield', to: '/settings/security' },
    ],
  },
  {
    label: '日志与报告',
    icon: 'pi pi-chart-line',
    items: [
      { label: '系统日志', icon: 'pi pi-book', to: '/logs' },
      { label: '报表中心', icon: 'pi pi-file', to: '/reports' },
    ],
  },
])
