import axios from 'axios';

/**
 * 创建 axios 实例，配置基础路径和拦截器
 */
const client = axios.create({
  baseURL: '/api',
  timeout: 10000,
  headers: {
    'Content-Type': 'application/json',
  },
});

// 请求拦截器：自动注入 Bearer Token
client.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('token');
    if (token && config.headers) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

// 响应拦截器：处理常见的错误码
client.interceptors.response.use(
  (response) => {
    return response.data;
  },
  (error) => {
    if (error.response && error.response.status === 401) {
      // 处理未授权，例如跳转登录
      console.error('未授权，请重新登录');
      localStorage.removeItem('token');
    }
    return Promise.reject(error);
  }
);

export default client;
