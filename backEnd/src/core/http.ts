/**
 * 创建一个 JSON 响应对象
 * @param data 要返回的 JSON 数据
 * @param status HTTP 状态码
 * @param headers 额外的响应头
 * @returns Response 对象
 */
export function json(data: unknown, status = 200, headers: Record<string, string> = {}): Response {
  return new Response(JSON.stringify(data), {
    status,
    headers: {
      "content-type": "application/json; charset=utf-8",
      ...headers,
    },
  });
}

/**
 * 创建一个文本响应对象
 * @param text 文本内容
 * @param status HTTP 状态码
 * @param headers 额外的响应头
 * @returns Response 对象
 */
export function text(text: string, status = 200, headers: Record<string, string> = {}): Response {
  return new Response(text, {
    status,
    headers: {
      "content-type": "text/plain; charset=utf-8",
      ...headers,
    },
  });
}

/**
 * 解析请求体为 JSON
 * @param req Request 对象
 * @returns 解析后的对象
 */
export async function parseJson<T = unknown>(req: Request): Promise<T> {
  const contentType = req.headers.get("content-type") || "";
  if (!contentType.includes("application/json")) {
    throw new Error("Expected application/json request");
  }
  return (await req.json()) as T;
}

export type Query = Record<string, string | string[]>;

export interface HttpContext {
  req: Request;
  url: URL;
  params: Record<string, string>;
  query: Query;
  ip?: string;
}

/**
 * 解析 URL 查询参数为对象
 * @param url URL 对象
 * @returns 查询参数对象
 */
export function parseQuery(url: URL): Query {
  const q: Query = {};
  for (const [key, value] of url.searchParams.entries()) {
    if (q[key]) {
      const current = q[key];
      q[key] = Array.isArray(current) ? [...current, value] : [current as string, value];
    } else {
      q[key] = value;
    }
  }
  return q;
}

/**
 * 生成标准的 CORS 响应头
 * @param origin 请求源
 * @returns 响应头对象
 */
export function corsHeaders(origin: string | null): Record<string, string> {
  return {
    "access-control-allow-origin": origin ?? "*",
    "access-control-allow-headers": "content-type, authorization",
    "access-control-allow-methods": "GET,POST,PUT,PATCH,DELETE,OPTIONS",
  };
}
