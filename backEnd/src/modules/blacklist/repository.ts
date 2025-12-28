import type { Database } from "bun:sqlite";

export interface BlacklistEntry {
  id: number;
  ip: string;
  reason: string;
  blocked_at: string;
  expires_at: string | null;
  is_active: number;
}

export class BlacklistRepository {
  constructor(private db: Database) {}

  /**
   * 检查 IP 是否在黑名单中且未过期
   * @param ip 待检查的 IP
   * @returns 是否被封禁
   */
  isIpBlocked(ip: string): boolean {
    const stmt = this.db.query(`
      SELECT 1 FROM ip_blacklist 
      WHERE ip = $ip 
      AND is_active = 1 
      AND (expires_at IS NULL OR expires_at > datetime('now'))
      LIMIT 1
    `);
    return stmt.get({ $ip: ip }) !== null;
  }

  /**
   * 将 IP 加入黑名单
   * @param ip IP 地址
   * @param reason 封禁原因
   * @param durationMinutes 封禁时长 (分钟)，null 表示永久
   */
  addIp(ip: string, reason: string, durationMinutes: number | null): void {
    const expiresAt = durationMinutes ? new Date(Date.now() + durationMinutes * 60000).toISOString() : null;

    const stmt = this.db.query(`
      INSERT OR REPLACE INTO ip_blacklist (ip, reason, expires_at, is_active, blocked_at)
      VALUES ($ip, $reason, $expires_at, 1, datetime('now'))
    `);
    stmt.run({ $ip: ip, $reason: reason, $expires_at: expiresAt });
  }

  /**
   * 解封 IP
   * @param ip IP 地址
   */
  unblockIp(ip: string): void {
    const stmt = this.db.query(`UPDATE ip_blacklist SET is_active = 0 WHERE ip = $ip`);
    stmt.run({ $ip: ip });
  }

  /**
   * 清理已过期的黑名单记录
   */
  cleanupExpired(): void {
    const stmt = this.db.query(`DELETE FROM ip_blacklist WHERE expires_at IS NOT NULL AND expires_at < datetime('now')`);
    stmt.run();
  }
}
