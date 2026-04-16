import api from './axios'

export const forumApi = {
  getThreads: (courseId, params) => api.get(`/courses/${courseId}/forum`, { params }),
  getThread: (courseId, threadId) => api.get(`/courses/${courseId}/forum/${threadId}`),
  createThread: (courseId, data) => api.post(`/courses/${courseId}/forum`, data),
  reply: (courseId, threadId, data) => api.post(`/courses/${courseId}/forum/${threadId}/reply`, data),
  delete: (courseId, postId) => api.delete(`/courses/${courseId}/forum/${postId}`),
  pin: (courseId, threadId) => api.post(`/courses/${courseId}/forum/${threadId}/pin`),
  like: (courseId, postId) => api.post(`/courses/${courseId}/forum/${postId}/like`),
}

export const notificationsApi = {
  getAll: (params) => api.get('/notifications', { params }),
  getCount: () => api.get('/notifications/count'),
  markRead: (id) => api.post(`/notifications/${id}/read`),
  markAllRead: () => api.post('/notifications/read-all'),
  delete: (id) => api.delete(`/notifications/${id}`),
}

export const adminApi = {
  getStats: () => api.get('/admin/stats'),
  getUsers: (params) => api.get('/admin/users', { params }),
  setRole: (userId, role) => api.put(`/admin/users/${userId}/role`, { role }),
  toggleActive: (userId) => api.put(`/admin/users/${userId}/toggle-active`),
  getCourses: (params) => api.get('/admin/courses', { params }),
  togglePublish: (id) => api.post(`/admin/courses/${id}/toggle-publish`),
  deleteCourse: (id) => api.delete(`/admin/courses/${id}`),
}
