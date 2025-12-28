import { corsHeaders } from "@/core/http";
import { log } from "@/utils/systemUtil";
import type { Server } from "bun";

export type FetchHandler = (req: Request, server?: Server<any>) => Promise<Response> | Response;
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
 * 创建 HTTP 压缩中间件
 * 使用标准 Web API CompressionStream 支持 gzip 和 deflate 压缩
 * @returns 压缩中间件
 */
export function createCompressionMiddleware(): Middleware {
  return (next) => async (req, server) => {
    const res = await next(req, server);

    // #region 压缩逻辑 (简体中文说明：检查请求头并按需压缩响应体)
    // 1. 快速检查：如果已经压缩、无内容或状态码不需要 body，则跳过
    if (res.headers.has("Content-Encoding") || !res.body || res.status === 204 || res.status === 304) {
      return res;
    }

    // 2. 检查客户端支持的编码
    const acceptEncoding = req.headers.get("accept-encoding") ?? "";
    let encoding: "gzip" | "deflate" | null = null;

    if (acceptEncoding.includes("gzip")) {
      encoding = "gzip";
    } else if (acceptEncoding.includes("deflate")) {
      encoding = "deflate";
    }

    if (!encoding) return res;

    // 3. 检查 Content-Type，跳过已经压缩的媒体格式
    const contentType = res.headers.get("content-type") ?? "";
    const skipTypes = ["image/", "video/", "audio/", "application/zip", "application/x-gzip", "application/octet-stream"];
    if (skipTypes.some((type) => contentType.includes(type))) {
      return res;
    }

    // 4. 使用 CompressionStream 进行流式压缩
    const compressedStream = res.body.pipeThrough(new CompressionStream(encoding));
    const headers = new Headers(res.headers);
    headers.set("Content-Encoding", encoding);
    headers.delete("Content-Length"); // 压缩后长度变化，删除旧的 Content-Length

    return new Response(compressedStream, {
      status: res.status,
      headers,
    });
    // #endregion
  };
}

/**
 * 创建请求日志中间件，统一输出格式
 * @returns 日志中间件
 */
export function createLoggingMiddleware(): Middleware {
  return (next) => async (req, server) => {
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
  };
}

/**
 * 解析客户端 IP（优先使用代理头部）
 * @param req 请求对象
 * @returns 客户端 IP 字符串（可能为空）
 */
function resolveClientIp(req: Request, server?: Server<any>): string {
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
  const addr = server?.requestIP(req)?.address ?? "";
  return addr;
}
