import type { Route } from "@/core/router";
import { json, parseJson } from "@/core/http";
import { getDb } from "@/db/connection";
import { UserService } from "./service";

/**
 * 用户路由定义
 * @returns 路由数组
 */
export function userRoutes(): Route[] {
  const service = new UserService(getDb());
  return [
    {
      method: "GET",
      path: "/users",
      handler: () => json(service.list()),
    },
    {
      method: "GET",
      path: "/users/:id",
      handler: ({ params }) => {
        const id = Number(params.id);
        const user = service.get(id);
        return user ? json(user) : json({ message: "Not Found" }, 404);
      },
    },
    {
      method: "POST",
      path: "/users",
      handler: async ({ req }) => {
        const body = await parseJson<{ name: string; email: string }>(req);
        const user = service.create(body.name, body.email);
        return json(user, 201);
      },
    },
    {
      method: "PUT",
      path: "/users/:id",
      handler: async ({ req, params }) => {
        const id = Number(params.id);
        const body = await parseJson<{ name: string; email: string }>(req);
        const user = service.update(id, body.name, body.email);
        return user ? json(user) : json({ message: "Not Found" }, 404);
      },
    },
    {
      method: "DELETE",
      path: "/users/:id",
      handler: ({ params }) => {
        const id = Number(params.id);
        const ok = service.remove(id);
        return json({ success: ok });
      },
    },
  ];
}
