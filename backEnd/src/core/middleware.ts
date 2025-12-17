import { corsHeaders } from "@/core/http";
import { formatDateTime } from "@/utils/dateUtil";
import { log } from "@/utils/systemUtil";

export type FetchHandler = (req: Request) => Promise<Response> | Response;
export type Middleware = (next: FetchHandler) => FetchHandler;

/**
 * 组合中间件为单个处理函数
 * @param middlewares 中间件数组
 * @param handler 最终处理函数
 * @returns 组合后的处理函数
 */
export function compose(middlewares: Middleware[], handler: FetchHandler): FetchHandler {
  return middlewares.reduceRight((next, m) => m(next), handler);
}

/**
 * 创建请求日志中间件，统一输出格式
 * @returns 日志中间件
 */
export function createLoggingMiddleware(): Middleware {
  return (next) => async (req) => {
    const start = Date.now();
    const url = new URL(req.url);
    let res: Response;
    try {
      res = await next(req);
    } catch (err) {
      const dur = Date.now() - start;
      log("ERROR", "req_error", {
        method: req.method,
        path: url.pathname,
        query: url.search,
        duration_ms: dur,
        error: (err as Error)?.message ?? String(err),
        ip: resolveClientIp(req),
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
      path: url.pathname + url.search,
      duration_ms: duration,
      // ua: req.headers.get("user-agent") ?? "",
      // time: formatDateTime(new Date()),
      // ip: resolveClientIp(req),
    });
    return new Response(res.body, { status: res.status, headers: merged });
  };
}

/**
 * 解析客户端 IP（优先使用代理头部）
 * @param req 请求对象
 * @returns 客户端 IP 字符串（可能为空）
 */
function resolveClientIp(req: Request): string {
  const xf = req.headers.get("x-forwarded-for");
  if (xf && xf.length > 0) {
    const first = xf.split(",")[0].trim();
    if (first) return first;
  }
  const xr = req.headers.get("x-real-ip");
  if (xr && xr.trim()) return xr.trim();
  const fwd = req.headers.get("forwarded");
  if (fwd) {
    const m = fwd.match(/for="?([^;"\s]+)"?/i);
    if (m && m[1]) return m[1];
  }
  const cf = req.headers.get("cf-connecting-ip");
  if (cf && cf.trim()) return cf.trim();
  const cip = req.headers.get("x-client-ip");
  if (cip && cip.trim()) return cip.trim();
  return "";
}
