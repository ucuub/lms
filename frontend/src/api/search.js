import api from './axios'

export const searchApi = {
  search: (q, limit = 8) => api.get('/search', { params: { q, limit } }),
}
