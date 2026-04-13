import api from './axios'

export const practiceApi = {
  // Practice Quizzes
  getAll: ()            => api.get('/practice-quizzes'),
  create: (data)        => api.post('/practice-quizzes', data),
  remove: (id)          => api.delete(`/practice-quizzes/${id}`),

  // Attempts
  start:      (quizId)              => api.post(`/practice-quizzes/${quizId}/start`),
  submit:     (attemptId, answers)  => api.post(`/practice-attempts/${attemptId}/submit`, { answers }),
  getResult:  (attemptId)           => api.get(`/practice-attempts/${attemptId}/result`),
  myAttempts: ()                    => api.get('/practice-attempts/me'),
}
