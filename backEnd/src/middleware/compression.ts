import { type Middleware } from "@/core/middleware";

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
