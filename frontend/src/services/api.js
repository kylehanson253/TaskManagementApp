import axios from 'axios'

const api = axios.create({
  baseURL: '/api',
  headers: { 'Content-Type': 'application/json' },
  withCredentials: true, // include httpOnly cookies on every request
})

// Attach JWT access token from localStorage on every request
api.interceptors.request.use((config) => {
  const token = localStorage.getItem('token')
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

// Silent renewal — on 401, attempt one token refresh then retry the original request.
// All requests that arrive while a refresh is in progress are queued and replayed
// together once the new access token is available.
let isRefreshing = false
let failedQueue = []

const processQueue = (error) => {
  failedQueue.forEach(({ resolve, reject }) => {
    if (error) reject(error)
    else resolve()
  })
  failedQueue = []
}

api.interceptors.response.use(
  (res) => res,
  async (err) => {
    const original = err.config

    const isAuthEndpoint =
      original.url.includes('/auth/refresh') ||
      original.url.includes('/auth/revoke')

    if (err.response?.status === 401 && !original._retry && !isAuthEndpoint) {
      // Another refresh already in progress — queue this request
      if (isRefreshing) {
        return new Promise((resolve, reject) => {
          failedQueue.push({ resolve, reject })
        })
          .then(() => api(original))
          .catch((e) => Promise.reject(e))
      }

      original._retry = true
      isRefreshing = true

      try {
        // Cookie is sent automatically — no body needed
        const { data } = await api.post('/auth/refresh')
        localStorage.setItem('token', data.token)
        processQueue(null)
        return api(original) // replay the original request with new token
      } catch (refreshError) {
        processQueue(refreshError)
        localStorage.removeItem('token')
        localStorage.removeItem('user')
        window.location.href = '/login'
        return Promise.reject(refreshError)
      } finally {
        isRefreshing = false
      }
    }

    return Promise.reject(err)
  }
)

export const authApi = {
  login: (email, password) => api.post('/auth/login', { email, password }),
  register: (data) => api.post('/auth/register', data),
  refresh: () => api.post('/auth/refresh'),   // cookie sent automatically
  revoke: () => api.post('/auth/revoke'),     // cookie sent automatically
}

export const tasksApi = {
  getAll: () => api.get('/tasks'),
  getById: (id) => api.get(`/tasks/${id}`),
  getSummary: () => api.get('/tasks/summary'),
  create: (data) => api.post('/tasks', data),
  update: (id, data) => api.put(`/tasks/${id}`, data),
  complete: (id) => api.patch(`/tasks/${id}/complete`),
  delete: (id) => api.delete(`/tasks/${id}`),
}

export const usersApi = {
  getAll: () => api.get('/users'),
  getById: (id) => api.get(`/users/${id}`),
  create: (data) => api.post('/users', data),
  update: (id, data) => api.put(`/users/${id}`, data),
  delete: (id) => api.delete(`/users/${id}`),
}

export default api
