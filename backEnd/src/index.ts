import { Router } from "@/core/router";
import { json } from "@/core/http";
import { getDb, closeDb } from "@/db/connection";
import { runMigrations } from "@/db/migrator";
import { healthRoutes } from "@/modules/health/routes";
import { userRoutes } from "@/modules/users/routes";
import { statusRoutes } from "@/modules/status/routes";
import { compose, createLoggingMiddleware } from "@/core/middleware";
import { createStaticMiddleware } from "@/core/static";

/**
 * 构建并启动 HTTP 服务
 */
async function main() {
  // 初始化数据库并执行迁移
  getDb();
  runMigrations();

  // 注册路由
  const router = new Router();
  router.setNotFound(() => json({ message: "Route Not Found" }, 404));
  router.registerAll(healthRoutes(), "/api");
  router.registerAll(userRoutes(), "/api");
  router.registerAll(statusRoutes(), "/api");

  // 读取端口配置
  const port = Number(process.env.PORT ?? 5172);

  // 启动 Bun HTTP 服务
  const server = Bun.serve({
    port,
    /**
     * 统一入口：中间件链路 + 路由派发
     * @param req 请求对象
     * @returns 响应对象
     */
    fetch: (req, srv) =>
      compose([createLoggingMiddleware(), createStaticMiddleware()], (innerReq, innerSrv) =>
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

  console.log(`[server] listening on http://localhost:${server.port}`);

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
