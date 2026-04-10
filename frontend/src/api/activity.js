import api from './axios'

export const activityApi = {
  getMyActivity: (limit = 30) => api.get('/activity/me', { params: { limit } }),
  getCourseActivity: (courseId, limit = 50) => api.get(`/activity/course/${courseId}`, { params: { limit } }),
}
