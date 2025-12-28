import type { Server } from "bun";

// #region 类型定义 (简体中文说明：定义中间件相关的核心类型)
export type FetchHandler = (req: Request, server?: Server<any>) => Promise<Response> | Response;
export type Middleware = (next: FetchHandler) => FetchHandler;
// #endregion

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
 * 解析客户端 IP（优先使用代理头部）
 * @param req 请求对象
 * @returns 客户端 IP 字符串（可能为空）
 */
export function resolveClientIp(req: Request, server?: Server<any>): string {
  // #region IP 解析逻辑 (简体中文说明：从各类代理头中提取真实客户端 IP)
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
  // #endregion
}
