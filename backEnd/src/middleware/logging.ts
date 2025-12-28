import { corsHeaders } from "@/core/http";
import { log } from "@/utils/systemUtil";
import { resolveClientIp, type Middleware } from "@/core/middleware";

/**
 * 创建请求日志中间件，统一输出格式
 * @returns 日志中间件
 */
export function createLoggingMiddleware(): Middleware {
  return (next) => async (req, server) => {
    // #region 日志记录逻辑 (简体中文说明：记录请求基本信息、耗时及 IP)
    const start = Date.now();
    const url = new URL(req.url);
    let res: Response;
    try {
      res = await next(req, server);
    } catch (err) {
      const dur = Date.now() - start;
      log("ERROR", "req_error", {
        method: req.method,
        path: url.pathname,
        query: url.search,
        duration_ms: dur,
        error: (err as Error)?.message ?? String(err),
        ip: resolveClientIp(req, server),
      });
      throw err;
    }
    const duration = Date.now() - start;
    const origin = req.headers.get("origin");
    // 补充 CORS 响应头（确保静态等响应也有相同头）
    const merged = new Headers(res.headers);
    const cors = corsHeaders(origin);
    for (const [k, v] of Object.entries(cors)) merged.set(k, v);
    // 统一的 KV 格式日志
    log("INFO", "req", {
      method: req.method,
      status: res.status,
      ip: resolveClientIp(req, server),
      path: url.pathname + url.search,
      duration_ms: duration,
    });
    return new Response(res.body, { status: res.status, headers: merged });
    // #endregion
  };
}
