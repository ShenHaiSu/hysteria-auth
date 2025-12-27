import type { Route } from "@/core/router";
import { json } from "@/core/http";
import { StatusService } from "@/modules/status/service";

// #region 路由定义：/status/server 与 /status/client
/**
 * 状态模块路由
 * @returns 路由定义数组
 */
export function statusRoutes(): Route[] {
  const service = new StatusService();
  return [
    {
      method: "GET",
      path: "/status/server",
      handler: () => json(service.getServerStatus()),
    },
    {
      method: "GET",
      path: "/status/client",
      handler: (ctx) => json(service.getClientInfo(ctx)),
    },
  ];
}
// #endregion
