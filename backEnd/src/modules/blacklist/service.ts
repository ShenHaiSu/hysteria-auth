import type { Database } from "bun:sqlite";
import { BlacklistRepository } from "./repository";

export class BlacklistService {
  private repo: BlacklistRepository;

  constructor(db: Database) {
    this.repo = new BlacklistRepository(db);
  }

  /**
   * 判断 IP 是否允许访问
   * @param ip 客户端 IP
   * @returns boolean
   */
  isAllowed(ip: string): boolean {
    return !this.repo.isIpBlocked(ip);
  }

  /**
   * 封禁 IP
   * @param ip 客户端 IP
   * @param reason 封禁原因
   * @param durationMinutes 封禁时长
   */
  blockIp(ip: string, reason: string, durationMinutes: number | null = 60): void {
    this.repo.addIp(ip, reason, durationMinutes);
  }

  /**
   * 执行后台清理任务
   */
  cronCleanup(): void {
    this.repo.cleanupExpired();
  }
}
