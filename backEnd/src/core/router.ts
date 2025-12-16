import { corsHeaders, HttpContext, json, parseQuery, text } from "#core/http";

export type Method = "GET" | "POST" | "PUT" | "PATCH" | "DELETE" | "OPTIONS";

export type Handler = (ctx: HttpContext) => Promise<Response> | Response;

export interface Route {
  method: Method;
  path: string;
  handler: Handler;
}

/**
 * 将路由模板转换为可匹配的段数组
 * @param path 路由路径模板，如 /users/:id
 * @returns 路由段数组
 */
function tokenize(path: string): string[] {
  return path.replace(/\/+$/, "").split("/").filter(Boolean);
}

/**
 * 根据路由模板匹配实际路径，提取参数
 * @param template 路由模板段数组
 * @param actual 实际路径段数组
 * @returns 是否匹配与参数结果
 */
function matchSegments(
  template: string[],
  actual: string[]
): { matched: boolean; params: Record<string, string> } {
  if (template.length !== actual.length) {
    return { matched: false, params: {} };
  }
  const params: Record<string, string> = {};
  for (let i = 0; i < template.length; i++) {
    const t = template[i];
    const a = actual[i];
    if (t.startsWith(":")) {
      const key = t.slice(1);
      params[key] = decodeURIComponent(a);
      continue;
    }
    if (t !== a) {
      return { matched: false, params: {} };
    }
  }
  return { matched: true, params };
}

/**
 * 简易路由器，支持 :param 参数匹配
 */
export class Router {
  private routes: Array<{ method: Method; segments: string[]; handler: Handler }> = [];
  private notFoundHandler: Handler = () => json({ message: "Not Found" }, 404);

  /**
   * 注册单个路由
   * @param route 路由定义
   */
  register(route: Route): void {
    this.routes.push({
      method: route.method,
      segments: tokenize(route.path),
      handler: route.handler,
    });
  }

  /**
   * 批量注册路由
   * @param routes 路由数组
   * @param basePath 可选前缀路径
   */
  registerAll(routes: Route[], basePath = ""): void {
    const baseSegments = tokenize(basePath);
    for (const r of routes) {
      const segments = [...baseSegments, ...tokenize(r.path)];
      this.routes.push({ method: r.method, segments, handler: r.handler });
    }
  }

  /**
   * 设置自定义的 404 处理器
   * @param handler 处理器
   */
  setNotFound(handler: Handler): void {
    this.notFoundHandler = handler;
  }

  /**
   * 处理一个传入请求
   * @param req Request 对象
   * @returns Response 对象
   */
  async handle(req: Request): Promise<Response> {
    const url = new URL(req.url);
    const origin = req.headers.get("origin");

    // 预检请求处理
    if (req.method === "OPTIONS") {
      return text("", 200, corsHeaders(origin));
    }

    const pathSegments = tokenize(url.pathname);
    for (const route of this.routes) {
      if (route.method !== (req.method as Method)) continue;
      const result = matchSegments(route.segments, pathSegments);
      if (!result.matched) continue;
      const ctx: HttpContext = {
        req,
        url,
        params: result.params,
        query: parseQuery(url),
      };
      try {
        const res = await route.handler(ctx);
        const headers = corsHeaders(origin);
        // 合并 CORS 响应头
        const merged = new Headers(res.headers);
        for (const [k, v] of Object.entries(headers)) merged.set(k, v);
        return new Response(res.body, {
          status: res.status,
          headers: merged,
        });
      } catch (err) {
        console.error("[router] handler error:", err);
        return json({ message: "Internal Server Error" }, 500, corsHeaders(origin));
      }
    }
    return this.notFoundHandler({
      req,
      url,
      params: {},
      query: parseQuery(url),
    });
  }
}
