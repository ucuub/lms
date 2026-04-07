import api from './axios'

export const assignmentsApi = {
  getByCourse: (courseId) => api.get(`/courses/${courseId}/assignments`),
  getById: (id) => api.get(`/assignments/${id}`),
  create: (courseId, data) => api.post(`/courses/${courseId}/assignments`, data),
  update: (id, data) => api.put(`/assignments/${id}`, data),
  delete: (id) => api.delete(`/assignments/${id}`),

  submit: (id, textContent, file) => {
    const form = new FormData()
    if (textContent) form.append('textContent', textContent)
    if (file) form.append('file', file)
    return api.post(`/assignments/${id}/submit`, form)
  },
  getSubmissions: (id) => api.get(`/assignments/${id}/submissions`),
  gradeSubmission: (submissionId, data) => api.post(`/submissions/${submissionId}/grade`, data),
  mySubmission: (id) => api.get(`/assignments/${id}/my-submission`),
}

export const gradebookApi = {
  getCourse: (courseId) => api.get(`/courses/${courseId}/gradebook`),
  getMine: (courseId) => api.get(`/courses/${courseId}/gradebook/me`),
  exportCsv: (courseId) => api.get(`/courses/${courseId}/gradebook/export`, { responseType: 'blob' }),
}
