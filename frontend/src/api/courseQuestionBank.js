import api from './axios'

export const courseQuestionBankApi = {
  getAll:    (courseId, moduleId) => api.get(`/courses/${courseId}/question-bank`, { params: moduleId ? { moduleId } : {} }),
  getById:   (courseId, id)       => api.get(`/courses/${courseId}/question-bank/${id}`),
  create:    (courseId, data)     => api.post(`/courses/${courseId}/question-bank`, data),
  update:    (courseId, id, data) => api.put(`/courses/${courseId}/question-bank/${id}`, data),
  delete:    (courseId, id)       => api.delete(`/courses/${courseId}/question-bank/${id}`),
  importCsv: (courseId, file, moduleId) => {
    const form = new FormData()
    form.append('file', file)
    return api.post(
      `/courses/${courseId}/question-bank/import-csv${moduleId ? `?moduleId=${moduleId}` : ''}`,
      form,
      { headers: { 'Content-Type': 'multipart/form-data' } }
    )
  },
}
