export interface PageRequest {
  page: number
  pageSize: number
}

export interface PageResponse<T> {
  total: number
  page: number
  pageSize: number
  items: T[]
}

export interface ApiError {
  error: {
    code: string
    message: string
    requestId: string
  }
}
