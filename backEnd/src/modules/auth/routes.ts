import type { Route } from "@/core/router";
import { json, parseJson } from "@/core/http";
import { getDb } from "@/db/connection";
import { AuthService } from "./service";
import type { AuthRequest, LoginRequest } from "@/composable/auth/Auth";

/**
 * 授权路由：/api/auth
 * 仅支持 POST，接收 JSON 请求体 { addr, auth, tx }
 * 响应 JSON { ok: boolean, id: string }
 * 逻辑：
 * - 来源 IP 必须存在于 node_server 且 is_active=1，否则 404
 * - 请求体中的 auth 必须匹配某个激活用户的 proxy_password，否则 404
 * - 两者满足返回 200 与 { ok: true, id }
 */
export function authRoutes(): Route[] {
  const service = new AuthService(getDb());
  return [
    {
      method: "POST",
      path: "/auth",
      handler: async ({ req, ip }) => {
        if (!ip || !service.verifyNodeServer(ip)) {
          return json({ ok: false, id: "" }, 404);
        }
        let body: AuthRequest;
        try {
          body = await parseJson<AuthRequest>(req);
        } catch {
          return json({ ok: false, id: "" }, 404);
        }
        if (typeof body.addr !== "string" || typeof body.auth !== "string" || typeof body.tx !== "number") {
          return json({ ok: false, id: "" }, 404);
        }
        const res = service.validate(ip, body);
        return res.ok ? json(res, 200) : json(res, 404);
      },
    },
    {
      method: "POST",
      path: "/login",
      handler: async ({ req, ip }) => {
        let body: LoginRequest;
        try {
          body = await parseJson<LoginRequest>(req);
        } catch {
          return json({ message: "Bad Request" }, 400);
        }
        if (typeof body.username !== "string" || typeof body.login_password_md5 !== "string") {
          return json({ message: "Bad Request" }, 400);
        }
        const result = service.login(ip || "", body);
        if (!result) {
          return json({ message: "Unauthorized" }, 401);
        }
        return json(result);
      },
    },
  ];
}
