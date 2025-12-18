import type { Route } from "@/core/router";
import { json, parseJson } from "@/core/http";
import { getDb } from "@/db/connection";
import { UserService } from "./service";
import { AuthService } from "@/modules/auth/service";

/**
 * 用户路由定义
 * @returns 路由数组
 */
export function userRoutes(): Route[] {
  const db = getDb();
  const service = new UserService(db);
  const auth = new AuthService(db);
  return [
    {
      method: "GET",
      path: "/users/me",
      handler: ({ req }) => {
        const me = auth.getMe(req);
        return me ? json(me) : json({ message: "Unauthorized" }, 401);
      },
    },
    {
      method: "GET",
      path: "/users",
      handler: ({ req }) => {
        const session = auth.verifyRequestToken(req);
        if (!session) return json({ message: "Unauthorized" }, 401);
        if (session.permission !== "admin") return json({ message: "Forbidden" }, 403);
        return json(service.list());
      },
    },
    {
      method: "GET",
      path: "/users/:id",
      handler: ({ req, params }) => {
        const session = auth.verifyRequestToken(req);
        if (!session) return json({ message: "Unauthorized" }, 401);
        if (session.permission !== "admin") return json({ message: "Forbidden" }, 403);
        const id = Number(params.id);
        const user = service.get(id);
        return user ? json(user) : json({ message: "Not Found" }, 404);
      },
    },
    {
      method: "POST",
      path: "/users",
      handler: async ({ req }) => {
        const session = auth.verifyRequestToken(req);
        if (!session) return json({ message: "Unauthorized" }, 401);
        if (session.permission !== "admin") return json({ message: "Forbidden" }, 403);
        const body = await parseJson<{ name: string; email: string }>(req);
        const user = service.create(body.name, body.email);
        return json(user, 201);
      },
    },
    {
      method: "PUT",
      path: "/users/:id",
      handler: async ({ req, params }) => {
        const session = auth.verifyRequestToken(req);
        if (!session) return json({ message: "Unauthorized" }, 401);
        if (session.permission !== "admin") return json({ message: "Forbidden" }, 403);
        const id = Number(params.id);
        const body = await parseJson<{ name: string; email: string }>(req);
        const user = service.update(id, body.name, body.email);
        return user ? json(user) : json({ message: "Not Found" }, 404);
      },
    },
    {
      method: "DELETE",
      path: "/users/:id",
      handler: ({ req, params }) => {
        const session = auth.verifyRequestToken(req);
        if (!session) return json({ message: "Unauthorized" }, 401);
        if (session.permission !== "admin") return json({ message: "Forbidden" }, 403);
        const id = Number(params.id);
        const ok = service.remove(id);
        return json({ success: ok });
      },
    },
  ];
}
