import crypto from "node:crypto";

/**
 * Base64URL 编码
 * @param buf 输入 Buffer
 * @returns Base64URL 字符串
 */
function b64url(buf: Buffer): string {
  return buf
    .toString("base64")
    .replace(/=/g, "")
    .replace(/\+/g, "-")
    .replace(/\//g, "_");
}

/**
 * Base64URL 解码
 * @param str Base64URL 字符串
 * @returns Buffer
 */
function b64urlDecode(str: string): Buffer {
  const pad = str.length % 4 === 0 ? "" : "=".repeat(4 - (str.length % 4));
  const base64 = str.replace(/-/g, "+").replace(/_/g, "/") + pad;
  return Buffer.from(base64, "base64");
}

export interface SessionPayload {
  uid: number;
  username: string | null;
  permission: "admin" | "user";
  iat: number;
  exp: number;
}

/**
 * 生成 HS256 签名令牌（简易 JWT）
 * @param payload 负载对象
 * @param secret 密钥
 * @returns 令牌字符串
 */
export function signToken(payload: Omit<SessionPayload, "iat" | "exp">, secret: string, expiresInSec: number): string {
  const header = { alg: "HS256", typ: "JWT" };
  const now = Math.floor(Date.now() / 1000);
  const body: SessionPayload = { ...payload, iat: now, exp: now + expiresInSec };
  const h = b64url(Buffer.from(JSON.stringify(header)));
  const p = b64url(Buffer.from(JSON.stringify(body)));
  const data = `${h}.${p}`;
  const sig = crypto.createHmac("sha256", secret).update(data).digest();
  return `${data}.${b64url(sig)}`;
}

/**
 * 校验 HS256 令牌并返回负载
 * @param token 令牌字符串
 * @param secret 密钥
 * @returns 负载对象或 null
 */
export function verifyToken(token: string, secret: string): SessionPayload | null {
  const parts = token.split(".");
  if (parts.length !== 3) return null;
  const [h, p, s] = parts;
  const data = `${h}.${p}`;
  const expected = b64url(crypto.createHmac("sha256", secret).update(data).digest());
  if (expected !== s) return null;
  const payloadJson = b64urlDecode(p).toString("utf-8");
  let payload: SessionPayload;
  try {
    payload = JSON.parse(payloadJson);
  } catch {
    return null;
  }
  const now = Math.floor(Date.now() / 1000);
  if (typeof payload.exp !== "number" || now >= payload.exp) return null;
  return payload;
}

/**
 * 从 Authorization 头部提取 Bearer 令牌
 * @param req Request 对象
 * @returns 令牌字符串或空字符串
 */
export function getBearerToken(req: Request): string {
  const auth = req.headers.get("authorization") ?? "";
  const m = auth.match(/^Bearer\s+(.+)$/i);
  return m?.[1] ?? "";
}
