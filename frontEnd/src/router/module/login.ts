import type { RouteRecordRaw } from 'vue-router'

// #region 登录路由
/**
 * 登录页面路由定义
 */
const loginRoute: RouteRecordRaw = {
  path: '/login',
  name: 'login',
  component: () => import('@/view/Login.vue'),
  meta: { guest: true },
}
// #endregion

export default loginRoute
