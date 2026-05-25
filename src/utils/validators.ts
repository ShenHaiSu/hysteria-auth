/**
 * 表单校验规则集合
 */

export const validators = {
  required: (value: unknown): boolean | string => {
    if (value === undefined || value === null || value === '') {
      return 'validation.required'
    }
    if (Array.isArray(value) && value.length === 0) {
      return 'validation.required'
    }
    return true
  },

  minLength:
    (min: number) =>
    (value: string): boolean | string => {
      if (!value || value.length < min) {
        return 'validation.minLength'
      }
      return true
    },

  maxLength:
    (max: number) =>
    (value: string): boolean | string => {
      if (value && value.length > max) {
        return 'validation.maxLength'
      }
      return true
    },

  minValue:
    (min: number) =>
    (value: number): boolean | string => {
      if (value !== undefined && value !== null && value < min) {
        return 'validation.minValue'
      }
      return true
    },

  maxValue:
    (max: number) =>
    (value: number): boolean | string => {
      if (value !== undefined && value !== null && value > max) {
        return 'validation.maxValue'
      }
      return true
    },

  pattern:
    (regex: RegExp) =>
    (value: string): boolean | string => {
      if (value && !regex.test(value)) {
        return 'validation.pattern'
      }
      return true
    },
}
