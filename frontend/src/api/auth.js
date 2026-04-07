import api from './axios'

export const authApi = {
  me: () => api.get('/auth/me'),
  sync: () => api.post('/auth/sync'),
  updateProfile: (data) => api.put('/auth/profile', data),
  uploadAvatar: (file) => {
    const form = new FormData()
    form.append('file', file)
    return api.post('/auth/avatar', form)
  },
}
