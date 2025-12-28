import type { Route } from "@/core/router";
import { json, parseJson } from "@/core/http";
import { getDb } from "@/db/connection";
import { NodeService } from "@/modules/nodes/service";
import { AuthService } from "@/modules/auth/service";

/**
 * 节点管理路由定义
 * @returns 路由数组
 */
export function nodeRoutes(): Route[] {
  const service = new NodeService(getDb());
  const auth = new AuthService(getDb());
  return [
    {
      method: "GET",
      path: "/nodes",
      handler: ({ req, query }) => {
        const session = auth.verifyRequestToken(req);
        if (!session) return json({ message: "Unauthorized" }, 401);
        const filters = {
          server_group: typeof query.server_group === "string" ? query.server_group : undefined,
          ip_address: typeof query.ip_address === "string" ? query.ip_address : undefined,
          domain: typeof query.domain === "string" ? query.domain : undefined,
          is_active:
            typeof query.is_active === "string" ? Number(query.is_active) : undefined,
          expire_from:
            typeof query.expire_from === "string" ? Number(query.expire_from) : undefined,
          expire_to:
            typeof query.expire_to === "string" ? Number(query.expire_to) : undefined,
        };
        return json(service.list(req, filters));
      },
    },
    {
      method: "POST",
      path: "/nodes",
      handler: async ({ req }) => {
        const session = auth.verifyRequestToken(req);
        if (!session) return json({ message: "Unauthorized" }, 401);
        if (session.permission !== "admin") return json({ message: "Forbidden" }, 403);
        const body = await parseJson<any>(req);
        const created = service.create(req, {
          ip_address: String(body.ip_address ?? ""),
          domain: body.domain ? String(body.domain) : null,
          server_group: String(body.server_group ?? ""),
          salamander: String(body.salamander ?? ""),
          idc_name: body.idc_name ? String(body.idc_name) : null,
          rent_ts: body.rent_ts ? Number(body.rent_ts) : Math.floor(Date.now() / 1000),
          expire_ts: body.expire_ts ? Number(body.expire_ts) : null,
          fee: body.fee ? Number(body.fee) : 0,
          proxy_port: String(body.proxy_port ?? ""),
          server_port: body.server_port ? Number(body.server_port) : null,
          note1: body.note1 ? String(body.note1) : null,
          note2: body.note2 ? String(body.note2) : null,
          note3: body.note3 ? String(body.note3) : null,
          note4: body.note4 ? String(body.note4) : null,
          is_active: body.is_active ? Number(body.is_active) : 1,
        });
        if (!created) return json({ message: "Forbidden" }, 403);
        return json(created, 201);
      },
    },
    {
      method: "PUT",
      path: "/nodes/:id",
      handler: async ({ req, params }) => {
        const session = auth.verifyRequestToken(req);
        if (!session) return json({ message: "Unauthorized" }, 401);
        if (session.permission !== "admin") return json({ message: "Forbidden" }, 403);
        const id = Number(params.id);
        const patch = await parseJson<any>(req);
        const updated = service.update(req, id, patch);
        if (!updated) return json({ message: "Forbidden" }, 403);
        return json(updated);
      },
    },
    {
      method: "DELETE",
      path: "/nodes/:id",
      handler: ({ req, params }) => {
        const session = auth.verifyRequestToken(req);
        if (!session) return json({ message: "Unauthorized" }, 401);
        if (session.permission !== "admin") return json({ message: "Forbidden" }, 403);
        const id = Number(params.id);
        const ok = service.remove(req, id);
        if (!ok) return json({ message: "Forbidden" }, 403);
        return json({ success: true });
      },
    },
    {
      method: "GET",
      path: "/proxy/config",
      handler: ({ req }) => {
        const me = auth.getMe(req);
        if (!me) return json({ message: "Unauthorized" }, 401);
        const now = Math.floor(Date.now() / 1000);
        const notExpired =
          me.proxy_expire_ts === null || me.proxy_expire_ts === 0 || (me.proxy_expire_ts ?? 0) > now;
        if (me.is_active !== 1 || !notExpired) {
          return json({ message: "Forbidden" }, 403);
        }
        const list = service.proxyConfig();
        return json({ items: list });
      },
    },
  ];
}
