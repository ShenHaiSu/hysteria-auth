import { resolveClientIp, type Middleware } from "@/core/middleware";
import { log } from "@/utils/systemUtil";
import { json } from "@/core/http";
import type { BlacklistService } from "@/modules/blacklist/service";

interface RateLimitStore {
  [ip: string]: {
    count: number;
    resetTime: number;
  };
}

/**
 * 创建频率限制中间件 (Rate Limiter)
 * @param blacklistService 黑名单服务，用于持久化封禁
 * @param windowMs 时间窗口 (毫秒)
 * @param max 请求次数上限
 * @returns 中间件函数
 */
export function createRateLimitMiddleware(
  blacklistService: BlacklistService,
  windowMs: number = 60000, 
  max: number = 60
): Middleware {
  // #region 内存存储 (简体中文说明：使用内存对象记录 IP 请求频率)
  const store: RateLimitStore = {};
  // #endregion

  // 定期清理内存记录
  setInterval(() => {
    const now = Date.now();
    for (const ip in store) {
      if (now > store[ip].resetTime) {
        delete store[ip];
      }
    }
  }, windowMs);

  return (next) => async (req, server) => {
    const ip = resolveClientIp(req, server);

    // #region 黑名单预检 (简体中文说明：如果 IP 已在持久化黑名单中，直接拒绝)
    if (!blacklistService.isAllowed(ip)) {
      return json({ message: "Access Denied: IP Blocked" }, 403);
    }
    // #endregion

    // #region 限流检查逻辑
    const now = Date.now();
    if (!store[ip] || now > store[ip].resetTime) {
      store[ip] = {
        count: 1,
        resetTime: now + windowMs,
      };
    } else {
      store[ip].count++;
    }

    if (store[ip].count > max) {
      log("WARN", "rate_limit_exceeded", { ip, count: store[ip].count });
      
      // #region 持久化封禁触发 (简体中文说明：如果单 IP 频率极高，触发 1 小时持久化封禁)
      if (store[ip].count > max * 2) {
        blacklistService.blockIp(ip, "Extreme Rate Limit Exceeded", 60);
      }
      // #endregion

      return json({ message: "Too Many Requests" }, 429);
    }

    return next(req, server);
    // #endregion
  };
}
