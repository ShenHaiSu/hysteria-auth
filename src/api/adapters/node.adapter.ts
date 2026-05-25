import type { NodeDto } from '@/types/node.types'

/**
 * 节点数据适配器 — 当前仅做透传，保留扩展点
 */
export function adaptNodeDto(raw: NodeDto): NodeDto {
  return raw
}
