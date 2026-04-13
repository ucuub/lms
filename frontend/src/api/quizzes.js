import api from './axios'

export const quizzesApi = {
  getByCourse: (courseId) => api.get(`/courses/${courseId}/quizzes`),
  getAvailable: () => api.get('/quizzes/available'),
  getById: (id) => api.get(`/quizzes/${id}`),
  create: (courseId, data) => api.post(`/courses/${courseId}/quizzes`, data),
  update: (id, data) => api.put(`/quizzes/${id}`, data),
  delete: (id) => api.delete(`/quizzes/${id}`),

  // Questions
  getQuestions: (quizId) => api.get(`/quizzes/${quizId}/questions`),
  addQuestion: (quizId, data) => api.post(`/quizzes/${quizId}/questions`, data),
  updateQuestion: (id, data) => api.put(`/questions/${id}`, data),
  deleteQuestion: (id) => api.delete(`/questions/${id}`),
  importFromBank: (quizId, ids) => api.post(`/quizzes/${quizId}/import-from-bank`, { questionBankIds: ids }),

  // Attempts
  start: (quizId) => api.post(`/quizzes/${quizId}/start`),
  submit: (attemptId, answers) => api.post(`/attempts/${attemptId}/submit`, { answers }),
  getResult: (attemptId) => api.get(`/attempts/${attemptId}/result`),
  gradeEssay: (answerId, data) => api.post(`/attempt-answers/${answerId}/grade`, data),

  // Question bank
  getBank: (params) => api.get('/question-bank', { params }),
  addToBank: (data) => api.post('/question-bank', data),
  deleteFromBank: (id) => api.delete(`/question-bank/${id}`),
}
