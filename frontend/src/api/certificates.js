import api from './axios'

export const certificatesApi = {
  // Completion
  getCompletionStatus: (courseId) => api.get(`/courses/${courseId}/completion-status`),
  getCompletionRule:   (courseId) => api.get(`/courses/${courseId}/completion-rule`),
  claimCertificate:   (courseId) => api.post(`/courses/${courseId}/certificate/claim`),

  // My certificates
  getMyCertificates: () => api.get('/certificates/me'),
  getCertificate:    (courseId) => api.get(`/courses/${courseId}/certificate`),
  verify:            (number) => api.get(`/certificates/verify/${number}`),
}

export const calendarApi = {
  getEvents: (from, to) => api.get('/calendar', { params: { from, to } }),
  createEvent: (data)    => api.post('/calendar', data),
  updateEvent: (id, data) => api.put(`/calendar/${id}`, data),
  deleteEvent: (id)      => api.delete(`/calendar/${id}`),
}
