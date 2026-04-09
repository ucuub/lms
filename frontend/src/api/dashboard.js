import api from './axios'

export const dashboardApi = {
  get: () => api.get('/dashboard'),
  getStudent: () => api.get('/dashboard/student'),
  getTeacher: () => api.get('/dashboard/teacher'),
}
