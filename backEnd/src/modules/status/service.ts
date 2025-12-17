import os from "node:os";
import type { HttpContext } from "@/core/http";

// #region 状态服务：聚合系统与请求信息
/**
 * 状态服务：提供服务器运行信息与客户端请求详情
 */
export class StatusService {
  /**
   * 收集服务器运行状态信息
   * @returns 服务器状态 JSON
   */
  getServerStatus() {
    const memTotal = os.totalmem();
    const memFree = os.freemem();
    const cpus = os.cpus();
    const nets = os.networkInterfaces();
    const addresses: Array<{ iface: string; address: string; family: string }> = [];
    for (const [iface, list] of Object.entries(nets)) {
      for (const item of list ?? []) {
        addresses.push({ iface, address: item.address, family: item.family });
      }
    }
    const envKeys = Object.keys(Bun.env ?? {});
    const procMem = process.memoryUsage();
    const procCpu = process.cpuUsage();
    return {
      runtime: {
        bun: {
          version: Bun.version,
        },
        node_compat: {
          platform: process.platform,
          arch: process.arch,
          pid: process.pid,
        },
      },
      os: {
        hostname: os.hostname(),
        platform: os.platform(),
        release: os.release(),
        arch: os.arch(),
        uptime_sec: os.uptime(),
      },
      cpu: {
        count: cpus.length,
        model: cpus[0]?.model ?? "",
        speed_mhz: cpus[0]?.speed ?? 0,
        loadavg_1_5_15: os.loadavg(),
        process_cpu: {
          user: procCpu.user,
          system: procCpu.system,
        },
      },
      memory: {
        total_bytes: memTotal,
        free_bytes: memFree,
        used_bytes: memTotal - memFree,
        process: {
          rss: procMem.rss,
          heapTotal: procMem.heapTotal,
          heapUsed: procMem.heapUsed,
          external: (procMem as any).external ?? 0,
        },
      },
      network: {
        interfaces: addresses,
      },
      env: {
        keys: envKeys,
        safe: {
          NODE_ENV: Bun.env.NODE_ENV ?? process.env.NODE_ENV ?? "",
          PORT: Bun.env.PORT ?? process.env.PORT ?? "",
        },
      },
      time: {
        now_iso: new Date().toISOString(),
        tz: Intl.DateTimeFormat().resolvedOptions().timeZone,
      },
    };
  }

  /**
   * 收集客户端请求的完整信息
   * @param ctx 请求上下文
   * @returns 客户端请求详情 JSON
   */
  getClientInfo(ctx: HttpContext) {
    const { req, url, query } = ctx;
    const headersObj: Record<string, string> = {};
    for (const [k, v] of req.headers.entries()) {
      headersObj[k.toLowerCase()] = v;
    }
    const cookies: Record<string, string> = {};
    const cookieHeader = headersObj["cookie"] ?? "";
    if (cookieHeader) {
      for (const part of cookieHeader.split(";")) {
        const [k, ...rest] = part.trim().split("=");
        if (!k) continue;
        cookies[k] = rest.join("=") ?? "";
      }
    }
    const ip =
      (ctx.ip && ctx.ip.length > 0 ? ctx.ip : null) ?? this.resolveClientIp(headersObj);
    return {
      method: req.method,
      url: url.toString(),
      path: url.pathname,
      query,
      search: url.search,
      ip,
      protocol: url.protocol.replace(":", ""),
      host: url.host,
      headers: headersObj,
      cookies,
      ua: headersObj["user-agent"] ?? "",
      origin: headersObj["origin"] ?? "",
      referer: headersObj["referer"] ?? "",
      forwarded: {
        x_forwarded_for: headersObj["x-forwarded-for"] ?? "",
        x_real_ip: headersObj["x-real-ip"] ?? "",
        forwarded: headersObj["forwarded"] ?? "",
        cf_connecting_ip: headersObj["cf-connecting-ip"] ?? "",
        x_client_ip: headersObj["x-client-ip"] ?? "",
      },
      time: {
        now_iso: new Date().toISOString(),
      },
    };
  }

  /**
   * 解析客户端 IP（结合常见代理头）
   * @param headers 归一化后的请求头
   * @returns 客户端 IP 字符串（可能为空）
   */
  private resolveClientIp(headers: Record<string, string>): string {
    const xf = headers["x-forwarded-for"];
    if (xf && xf.length > 0) {
      const first = xf.split(",")[0].trim();
      if (first) return first;
    }
    const xr = headers["x-real-ip"];
    if (xr && xr.trim()) return xr.trim();
    const fwd = headers["forwarded"];
    if (fwd) {
      const m = fwd.match(/for="?([^;"\s]+)"?/i);
      if (m && m[1]) return m[1];
    }
    const cf = headers["cf-connecting-ip"];
    if (cf && cf.trim()) return cf.trim();
    const cip = headers["x-client-ip"];
    if (cip && cip.trim()) return cip.trim();
    return "";
  }
}
// #endregion
