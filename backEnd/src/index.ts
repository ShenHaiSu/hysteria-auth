import { Router } from "@/core/router";
import { json } from "@/core/http";
import { getDb, closeDb } from "@/db/connection";
import { runMigrations } from "@/db/migrator";
// import { healthRoutes } from "@/modules/health/routes";
import { userRoutes } from "@/modules/users/routes";
import { statusRoutes } from "@/modules/status/routes";
import { authRoutes } from "@/modules/auth/routes";
import { AuthService } from "@/modules/auth/service";
import { nodeRoutes } from "@/modules/nodes/routes";
import { compose, createLoggingMiddleware, createCompressionMiddleware } from "@/core/middleware";
import { createStaticMiddleware } from "@/core/static";
import { existsSync, mkdirSync, readFileSync } from "node:fs";
import { join } from "node:path";

/**
 * 构建并启动 HTTP/HTTPS 服务
 */
async function main() {
  // 初始化数据库并执行迁移
  const db = getDb();
  runMigrations();

  // #region 启动后初始化逻辑 (简体中文说明：检查并初始化默认管理员)
  const authService = new AuthService(db);
  authService.initDefaultUser();
  // #endregion

  // #region 证书环境检查 (简体中文说明：检查并创建 cert 目录，配置 SSL 证书)
  // 定位 cert 文件夹（src 的同级目录，即项目根目录下的 cert）
  const certDir = join(process.cwd(), "cert");
  if (!existsSync(certDir)) {
    console.log(`[server] 正在创建证书目录: ${certDir}`);
    mkdirSync(certDir, { recursive: true });
  }

  const keyPath = join(certDir, "server.key");
  const crtPath = join(certDir, "server.crt");
  let tls = undefined;

  // 只有当 key 和 crt 同时存在时才尝试加载 TLS
  if (existsSync(keyPath) && existsSync(crtPath)) {
    try {
      tls = {
        key: readFileSync(keyPath),
        cert: readFileSync(crtPath),
      };
      console.log("[server] 检测到 SSL 证书，将以 HTTPS 模式启动");
    } catch (err) {
      console.error("[server] 加载证书失败，将回退到 HTTP 模式:", err);
    }
  } else {
    console.log("[server] 未检测到完整的证书文件 (server.key/server.crt)，使用 HTTP 模式");
  }
  // #endregion

  // 注册路由
  const router = new Router();
  router.setNotFound(() => json({ message: "Route Not Found" }, 404));
  // router.registerAll(healthRoutes(), "/api");
  router.registerAll(userRoutes(), "/api");
  router.registerAll(statusRoutes(), "/api");
  router.registerAll(authRoutes(), "/api");
  router.registerAll(nodeRoutes(), "/api");

  // 读取端口配置
  const port = Number(process.env.PORT ?? 5172);

  // 启动 Bun HTTP 服务
  const server = Bun.serve({
    port,
    tls,
    /**
     * 统一入口：中间件链路 + 路由派发
     * @param req 请求对象
     * @returns 响应对象
     */
    fetch: (req, srv) =>
      compose([createLoggingMiddleware(), createCompressionMiddleware(), createStaticMiddleware()], (innerReq, innerSrv) =>
        router.handle(innerReq, innerSrv)
      )(req, srv),
    /**
     * 关闭钩子：释放资源
     */
    error: (error) => {
      console.error("[server] fatal:", error);
      return json({ message: "Internal Server Error" }, 500);
    },
  });

  const protocol = tls ? "https" : "http";
  console.log(`[server] listening on ${protocol}://localhost:${server.port}`);

  // 优雅退出
  const shutdown = () => {
    console.log("[server] shutting down...");
    server.stop();
    closeDb();
    process.exit(0);
  };
  process.on("SIGINT", shutdown);
  process.on("SIGTERM", shutdown);
}

// 程序入口
main().catch((err) => {
  console.error("[bootstrap] failed:", err);
  process.exit(1);
});
