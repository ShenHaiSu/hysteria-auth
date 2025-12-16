import type { Database } from "bun:sqlite";
import { createUser, deleteUser, getUser, listUsers, updateUser } from "#users/repository";

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
   * @param name 名称
   * @param email 邮箱
   * @returns 新用户
   */
  create(name: string, email: string) {
    return createUser(this.db, name, email);
  }

  /**
   * 更新现有用户
   * @param id 用户 ID
   * @param name 名称
   * @param email 邮箱
   * @returns 更新后的用户或 null
   */
  update(id: number, name: string, email: string) {
    return updateUser(this.db, id, name, email);
  }

  /**
   * 删除用户
   * @param id 用户 ID
   * @returns 是否成功
   */
  remove(id: number) {
    return deleteUser(this.db, id);
  }
}
