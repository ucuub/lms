import api from './axios'

export const questionSetsApi = {
  getAll:           ()         => api.get('/question-sets'),
  getById:          (id)       => api.get(`/question-sets/${id}`),
  create:           (data)     => api.post('/question-sets', data),
  update:           (id, data) => api.put(`/question-sets/${id}`, data),
  delete:           (id)       => api.delete(`/question-sets/${id}`),

  // Question management
  addQuestion:      (id, data) => api.post(`/question-sets/${id}/questions`, data),
  importFromBank:   (id, ids)  => api.post(`/question-sets/${id}/import-from-bank`, { questionBankIds: ids }),
  updateQuestion:   (id, data) => api.put(`/question-set-questions/${id}`, data),
  deleteQuestion:   (id)       => api.delete(`/question-set-questions/${id}`),

  // Attempt
  start:            (id)       => api.post(`/question-sets/${id}/start`),
  submit:           (id, data) => api.post(`/question-set-attempts/${id}/submit`, data),
  getResult:        (id)       => api.get(`/question-set-attempts/${id}/result`),

  // Grading
  getAttempts:      (id)       => api.get(`/question-sets/${id}/attempts`),
  getAttemptDetail: (id, aId)  => api.get(`/question-sets/${id}/attempts/${aId}`),
  gradeEssay:       (aId, qId, data) => api.post(`/question-set-attempts/${aId}/grade-essay/${qId}`, data),
}
