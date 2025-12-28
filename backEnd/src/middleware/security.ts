import { resolveClientIp, type Middleware } from "@/core/middleware";
import { log } from "@/utils/systemUtil";
import { json } from "@/core/http";
import type { BlacklistService } from "@/modules/blacklist/service";

/**
 * 创建安全响应头中间件
 * 用于隐藏服务器特征，防止信息泄露
 * @returns 中间件函数
 */
export function createSecurityHeadersMiddleware(): Middleware {
  return (next) => async (req, server) => {
    // #region 响应头混淆逻辑 (简体中文说明：移除敏感头，伪装 Server 标识)
    const res = await next(req, server);
    const headers = new Headers(res.headers);

    headers.delete("X-Powered-By");
    headers.set("Server", "nginx");
    headers.set("X-Content-Type-Options", "nosniff");
    headers.set("X-XSS-Protection", "1; mode=block");

    return new Response(res.body, {
      status: res.status,
      headers,
    });
    // #endregion
  };
}

/**
 * 创建恶意爬虫拦截中间件 (UA 过滤)
 * @param blacklistService 黑名单服务
 * @returns 中间件函数
 */
export function createUABlockerMiddleware(blacklistService: BlacklistService): Middleware {
  const blockedUAs = [
    "curl",
    "python-requests",
    "go-http-client",
    "postmanruntime",
    "wget",
    "apache-httpclient",
    "sqlmap",
    "nmap",
  ];

  return (next) => async (req, server) => {
    const ip = resolveClientIp(req, server);
    
    // 预检黑名单
    if (!blacklistService.isAllowed(ip)) {
      return json({ message: "Forbidden" }, 403);
    }

    const ua = req.headers.get("user-agent")?.toLowerCase() ?? "";
    if (!ua || blockedUAs.some((blocked) => ua.includes(blocked))) {
      log("WARN", "security_blocked_ua", { ip, ua });
      // #region 持久化封禁 (简体中文说明：对恶意工具请求封禁 24 小时)
      blacklistService.blockIp(ip, `Malicious UA: ${ua}`, 1440);
      // #endregion
      return json({ message: "Forbidden" }, 403);
    }

    return next(req, server);
  };
}

/**
 * 创建蜜罐路由拦截中间件 (Honeypot)
 * @param blacklistService 黑名单服务
 * @returns 中间件函数
 */
export function createHoneypotMiddleware(blacklistService: BlacklistService): Middleware {
  const trapPaths = [
    ".env",
    ".git",
    "wp-login.php",
    "admin.php",
    "config.php",
    "phpinfo.php",
    "composer.json",
    ".vscode",
    "node_modules",
  ];

  return (next) => async (req, server) => {
    const ip = resolveClientIp(req, server);
    
    // 预检黑名单
    if (!blacklistService.isAllowed(ip)) {
      return json({ message: "Forbidden" }, 403);
    }

    const url = new URL(req.url);
    const path = url.pathname.toLowerCase();

    if (trapPaths.some((trap) => path.includes(trap))) {
      log("WARN", "security_honeypot_triggered", { ip, path });
      // #region 持久化封禁 (简体中文说明：触发蜜罐路径直接永久封禁，除非手动解封)
      blacklistService.blockIp(ip, `Honeypot Triggered: ${path}`, null); 
      // #endregion
      return json({ message: "Forbidden" }, 403);
    }

    return next(req, server);
  };
}
