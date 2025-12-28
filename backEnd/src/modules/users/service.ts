import type { Database } from "bun:sqlite";
import { createUser, deleteUser, getUser, listUsers, updateUser } from "@/modules/users/repository";

/**
 * 用户服务：聚合仓库能力，便于扩展验证、事件等
 * @param db 数据库连接
 */
export class UserService {
  private db: Database;
  constructor(db: Database) {
    this.db = db;
  }

  /**
   * 列出所有用户
   * @returns 用户数组
   */
  list() {
    return listUsers(this.db);
  }

  /**
   * 获取单个用户
   * @param id 用户 ID
   * @returns 用户或 null
   */
  get(id: number) {
    return getUser(this.db, id);
  }

  /**
   * 创建新用户
   * @param username 用户名
   * @param email 邮箱
   * @returns 新用户
   */
  create(username: string, email: string) {
    return createUser(this.db, username, email);
  }

  /**
   * 更新现有用户
   * @param id 用户 ID
   * @param username 用户名
   * @param email 邮箱
   * @param permission 权限
   * @param is_active 是否启用
   * @param proxy_password 代理密码
   * @param proxy_expire_ts 代理到期时间
   * @param login_password_md5 登录密码 (MD5)
   * @param operatorId 当前操作者 ID
   * @returns 更新后的用户或 null
   */
  update(
    id: number,
    username: string,
    email: string,
    permission: "admin" | "user",
    is_active: number,
    proxy_password: string,
    proxy_expire_ts: number | null,
    login_password_md5: string | undefined,
    operatorId: number
  ) {
    // 防御性逻辑：不允许管理员将自己的权限下调或禁用自己
    if (id === operatorId) {
      if (permission !== "admin") {
        throw new Error("Cannot downgrade your own permission");
      }
      if (is_active === 0) {
        throw new Error("Cannot deactivate your own account");
      }
    }
    return updateUser(
      this.db,
      id,
      username,
      email,
      permission,
      is_active,
      proxy_password,
      proxy_expire_ts,
      login_password_md5
    );
  }

  /**
   * 删除用户
   * @param id 用户 ID
   * @param operatorId 当前操作者 ID
   * @returns 是否成功
   */
  remove(id: number, operatorId: number) {
    // 防御性逻辑：不允许管理员删除自己的账号
    if (id === operatorId) {
      throw new Error("Cannot delete your own account");
    }
    return deleteUser(this.db, id);
  }
}
