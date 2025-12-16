import { corsHeaders } from "#core/http";
import { fileURLToPath } from "node:url";
import path from "node:path";
import fs from "node:fs";

/**
 * 解析静态资源根目录绝对路径
 * @returns 静态目录绝对路径
 */
export function resolveStaticRoot(): string {
  const root = fileURLToPath(new URL("../..", import.meta.url)); // backEnd/
  return path.resolve(root, "static");
}

/**
 * 根据扩展名推断 Content-Type
 * @param filePath 文件路径
 * @returns MIME 类型
 */
export function guessContentType(filePath: string): string {
  const ext = path.extname(filePath).toLowerCase();
  switch (ext) {
    case ".html":
      return "text/html; charset=utf-8";
    case ".js":
      return "application/javascript; charset=utf-8";
    case ".css":
      return "text/css; charset=utf-8";
    case ".json":
      return "application/json; charset=utf-8";
    case ".png":
      return "image/png";
    case ".jpg":
    case ".jpeg":
      return "image/jpeg";
    case ".gif":
      return "image/gif";
    case ".svg":
      return "image/svg+xml";
    case ".ico":
      return "image/x-icon";
    case ".woff":
      return "font/woff";
    case ".woff2":
      return "font/woff2";
    case ".ttf":
      return "font/ttf";
    case ".map":
      return "application/json; charset=utf-8";
    default:
      return "application/octet-stream";
  }
}

/**
 * 尝试处理静态资源请求
 * @param req 请求对象
 * @returns 找到文件时返回 Response，否则返回 null
 */
export async function tryServeStatic(req: Request): Promise<Response | null> {
  const url = new URL(req.url);
  // 仅处理非 /api 的路径
  if (url.pathname.startsWith("/api")) return null;
  const origin = req.headers.get("origin");
  const root = resolveStaticRoot();
  const staticRoot = path.resolve(root);
  const rawPath = decodeURIComponent(url.pathname);
  const resolved = path.resolve(staticRoot, "." + rawPath);
  // 防止路径穿越
  if (!resolved.startsWith(staticRoot)) {
    return new Response("Forbidden", { status: 403, headers: corsHeaders(origin) });
  }
  let target = resolved;
  if (!fs.existsSync(target) || fs.statSync(target).isDirectory()) {
    const indexFile = path.resolve(staticRoot, "index.html");
    if (fs.existsSync(indexFile)) {
      target = indexFile;
    } else {
      return new Response("Not Found", { status: 404, headers: corsHeaders(origin) });
    }
  }
  const file = Bun.file(target);
  return new Response(file, {
    status: 200,
    headers: {
      "content-type": guessContentType(target),
      ...corsHeaders(origin),
    },
  });
}

/**
 * 创建静态资源中间件
 * @returns 静态资源中间件
 */
export function createStaticMiddleware() {
  return (next: (req: Request) => Promise<Response> | Response) => async (req: Request) => {
    const res = await tryServeStatic(req);
    if (res) return res;
    return next(req);
  };
}

