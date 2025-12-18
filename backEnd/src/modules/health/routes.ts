/**
 * 演示使用, 不进行实际路由的挂载
 */

import { json } from "@/core/http";
import type { Route } from "@/core/router";

/**
 * 健康检查路由
 * @returns 路由定义数组
 */
export function healthRoutes(): Route[] {
  return [
    {
      method: "GET",
      path: "/health",
      handler: () => json({ status: "ok" }),
    },
  ];
}
