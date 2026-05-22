import type { UserDto } from '@/types/user.types'

interface RawUserDto extends Omit<UserDto, 'allowedNodes'> {
  allowedNodes: string
}

export function adaptUserDto(raw: RawUserDto): UserDto {
  return {
    ...raw,
    allowedNodes: raw.allowedNodes ? (JSON.parse(raw.allowedNodes) as string[]) : [],
  }
}
