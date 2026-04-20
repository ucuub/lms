import api from './axios'

export const courseQuestionBankApi = {
  getAll:   (courseId, moduleId) => api.get(`/courses/${courseId}/question-bank`, { params: moduleId ? { moduleId } : {} }),
  getById:  (courseId, id)       => api.get(`/courses/${courseId}/question-bank/${id}`),
  create:   (courseId, data)     => api.post(`/courses/${courseId}/question-bank`, data),
  update:   (courseId, id, data) => api.put(`/courses/${courseId}/question-bank/${id}`, data),
  delete:   (courseId, id)       => api.delete(`/courses/${courseId}/question-bank/${id}`),
}
