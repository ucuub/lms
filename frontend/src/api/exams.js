import api from './axios'

export const examsApi = {
  // Exam CRUD
  getAll: () => api.get('/exams'),
  getById: (id) => api.get(`/exams/${id}`),
  create: (data) => api.post('/exams', data),
  update: (id, data) => api.put(`/exams/${id}`, data),
  delete: (id) => api.delete(`/exams/${id}`),

  // Question management
  addQuestion: (examId, data) => api.post(`/exams/${examId}/questions`, data),
  updateQuestion: (id, data) => api.put(`/exam-questions/${id}`, data),
  deleteQuestion: (id) => api.delete(`/exam-questions/${id}`),

  // Attempt
  start: (examId) => api.post(`/exams/${examId}/start`),
  submit: (attemptId, answers) => api.post(`/exam-attempts/${attemptId}/submit`, { answers }),
  getResult: (attemptId) => api.get(`/exam-attempts/${attemptId}/result`),

  // Admin grading
  getAttempts: (examId) => api.get(`/exams/${examId}/attempts`),
  getAttemptDetail: (attemptId) => api.get(`/exam-attempts/${attemptId}/detail`),
  gradeEssay: (answerId, data) => api.post(`/exam-answers/${answerId}/grade`, data),
  gradeEssayByAttempt: (attemptId, questionId, data) =>
    api.post(`/exam-attempts/${attemptId}/grade-essay/${questionId}`, data),
}
