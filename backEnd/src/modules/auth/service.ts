import type { Database } from "bun:sqlite";
import type { ActiveUser, AuthRequest, AuthResponse, LoginRequest, LoginResponse, AuthUserInfo } from "@/composable/auth/Auth";
import {
  findActiveNodeServerByIp,
  findActiveUserByProxyPassword,
  findUserForLogin,
  updateLoginAudit,
  getUserInfoById,
  countUsers,
  createInitialAdmin,
  findUserWithPasswordByUsername,
  updateUserPassword,
} from "@/modules/auth/repository";
import { getBearerToken, signToken, verifyToken } from "@/modules/auth/token";

/**
 * 授权服务：校验节点服务器与用户代理密码
 * @param db 数据库连接
 */
export class AuthService {
  private db: Database;
  constructor(db: Database) {
    this.db = db;
  }

  /**
   * 读取令牌签名密钥
   * @returns 密钥字符串
   */
  getSecret(): string {
    return Bun.env.AUTH_SECRET ?? process.env.AUTH_SECRET ?? "change-me";
  }

  /**
   * 校验来源 IP 是否为激活的节点服务器
   * @param ip 来源 IP 地址
   * @returns 是否为激活节点
   */
  verifyNodeServer(ip: string): boolean {
    const nodeId = findActiveNodeServerByIp(this.db, ip);
    return nodeId !== null;
  }

  /**
   * 通过代理密码查找激活用户
   * @param proxyPassword 用户的代理密码
   * @returns 匹配到的用户或 null
   */
  findUserByProxyPassword(proxyPassword: string): ActiveUser | null {
    return findActiveUserByProxyPassword(this.db, proxyPassword);
  }

  /**
   * 计算用户的统计标识符
   * @param user 用户记录
   * @returns 标识符字符串（优先使用 username，缺省回退为 id）
   */
  computeIdentifier(user: ActiveUser): string {
    const uname = (user.username ?? "").trim();
    return uname.length > 0 ? uname : String(user.id);
  }

  /**
   * 执行完整授权校验逻辑
   * @param ip 来源 IP 地址
   * @param payload 请求体数据
   * @returns 授权结果（ok 与 id）
   */
  validate(ip: string, payload: AuthRequest): AuthResponse {
    if (!this.verifyNodeServer(ip)) {
      return { ok: false, id: "" };
    }
    const user = this.findUserByProxyPassword(payload.auth);
    if (!user) {
      return { ok: false, id: "" };
    }
    return { ok: true, id: this.computeIdentifier(user) };
  }

  // #region 认证与登录逻辑 (简体中文说明：处理用户授权、登录及初始化)
  /**
   * 初始化默认用户
   * 如果数据库中没有用户，则创建一个随机用户名的管理员，密码标记为 'empty'
   */
  initDefaultUser(): void {
    const count = countUsers(this.db);
    if (count === 0) {
      const randomName = "admin_" + Math.random().toString(36).substring(2, 8);
      const randomProxyPw = Math.random().toString(36).substring(2, 12);
      createInitialAdmin(this.db, randomName, randomProxyPw);
      console.log(`[auth] No users found. Created initial admin:`);
      console.log(`[auth] Username: ${randomName}`);
      console.log(`[auth] Password: empty (will be updated on first login)`);
      console.log(`[auth] Proxy Password: ${randomProxyPw}`);
    }
  }

  /**
   * 执行登录逻辑并签发令牌
   * @param ip 登录来源 IP
   * @param body 登录请求体
   * @returns 登录响应（令牌与用户信息），失败抛出错误
   */
  login(ip: string, body: LoginRequest): LoginResponse | null {
    // 1. 尝试常规登录
    let user = findUserForLogin(this.db, body.username, body.login_password_md5);

    // 2. 如果常规登录失败，检查是否为 'empty' 密码初始化逻辑
    if (!user) {
      const existingUser = findUserWithPasswordByUsername(this.db, body.username);
      if (existingUser && existingUser.login_password_md5 === "empty") {
        // 更新密码并重新获取用户信息
        updateUserPassword(this.db, existingUser.id, body.login_password_md5);
        console.log(`[auth] Initial password updated for user: ${body.username}`);
        user = findUserForLogin(this.db, body.username, body.login_password_md5);
      }
    }

    if (!user) return null;
    updateLoginAudit(this.db, user.id, ip);
    const token = signToken({ uid: user.id, username: user.username, permission: user.permission }, this.getSecret(), 2 * 60 * 60);
    return { token, user };
  }
  // #endregion

  /**
   * 解析并校验请求头中的 Bearer 令牌
   * @param req Request 对象
   * @returns 会话负载或 null
   */
  verifyRequestToken(req: Request) {
    const token = getBearerToken(req);
    if (!token) return null;
    return verifyToken(token, this.getSecret());
  }

  /**
   * 查询当前用户信息（基于令牌）
   * @param req Request 对象
   * @returns 用户信息或 null
   */
  getMe(req: Request): AuthUserInfo | null {
    const session = this.verifyRequestToken(req);
    if (!session) return null;
    return getUserInfoById(this.db, session.uid);
  }
}
