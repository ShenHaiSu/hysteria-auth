import type { Database } from "bun:sqlite";
import type { NodeServer, NodeFilters, ProxyConfigItem } from "@/composable/nodes/NodeServer";
import { listNodes, createNode, updateNode, deleteNode, listActiveProxyConfig } from "@/modules/nodes/repository";
import { verifyToken } from "@/modules/auth/token";

export type Role = "admin" | "user";

/**
 * 节点服务：封装节点查询与变更逻辑
 * @param db 数据库连接
 */
export class NodeService {
  private db: Database;
  constructor(db: Database) {
    this.db = db;
  }

  /**
   * 从请求头解析角色（默认 user）
   * @param req Request 对象
   * @returns 角色字符串
   */
  resolveRole(req: Request): Role {
    const auth = req.headers.get("authorization") ?? "";
    const m = auth.match(/^Bearer\s+(.+)$/i);
    if (!m) return "user";
    const secret = Bun.env.AUTH_SECRET ?? process.env.AUTH_SECRET ?? "change-me";
    const payload = verifyToken(m[1], secret);
    const role = payload?.permission ?? "user";
    return role === "admin" ? "admin" : "user";
  }

  /**
   * 列表查询（user 默认附加 is_active=1）
   * @param req Request 对象（用于解析角色）
   * @param filters 查询条件
   * @returns 节点数组
   */
  list(req: Request, filters: NodeFilters = {}): NodeServer[] {
    const role = this.resolveRole(req);
    const merged: NodeFilters = { ...filters };
    if (role === "user" && typeof merged.is_active !== "number") {
      merged.is_active = 1;
    }
    return listNodes(this.db, merged);
  }

  /**
   * 新增节点（admin）
   * @param req Request 对象（用于鉴权）
   * @param data 节点数据
   * @returns 新节点或 null
   */
  create(req: Request, data: Omit<NodeServer, "id" | "created_at" | "updated_at">): NodeServer | null {
    if (this.resolveRole(req) !== "admin") return null;
    return createNode(this.db, data);
  }

  /**
   * 更新节点（admin）
   * @param req Request 对象（用于鉴权）
   * @param id 节点 ID
   * @param patch 局部更新字段
   * @returns 更新后的节点或 null
   */
  update(req: Request, id: number, patch: Partial<NodeServer>): NodeServer | null {
    if (this.resolveRole(req) !== "admin") return null;
    return updateNode(this.db, id, patch);
  }

  /**
   * 删除节点（admin）
   * @param req Request 对象（用于鉴权）
   * @param id 节点 ID
   * @returns 是否成功
   */
  remove(req: Request, id: number): boolean {
    if (this.resolveRole(req) !== "admin") return false;
    return deleteNode(this.db, id);
  }

  /**
   * 生成代理配置（需用户令牌，且用户不检查到期由上游校验）
   * @returns 代理配置项数组
   */
  proxyConfig(): ProxyConfigItem[] {
    return listActiveProxyConfig(this.db);
  }
}
