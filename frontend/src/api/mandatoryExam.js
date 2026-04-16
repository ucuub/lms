import api from './axios'
import axios from 'axios'

// ── Management (requires Keycloak auth) ──────────────────────────────────────
export const mandatoryExamApi = {
  getAll:          ()              => api.get('/mandatory-exams'),
  getById:         (id)            => api.get(`/mandatory-exams/${id}`),
  create:          (data)          => api.post('/mandatory-exams', data),
  toggleActive:    (id)            => api.patch(`/mandatory-exams/${id}/toggle-active`),
  delete:          (id)            => api.delete(`/mandatory-exams/${id}`),

  addQuestion:     (id, data)      => api.post(`/mandatory-exams/${id}/questions`, data),
  deleteQuestion:  (examId, qId)   => api.delete(`/mandatory-exams/${examId}/questions/${qId}`),

  assign:          (id, data)      => api.post(`/mandatory-exams/${id}/assign`, data),
  unassign:        (id, userId)    => api.delete(`/mandatory-exams/${id}/assign/${userId}`),
  getAssignments:  (id)            => api.get(`/mandatory-exams/${id}/assignments`),

  generateLink:    (id, data)      => api.post(`/mandatory-exams/${id}/generate-link`, data),
}

// ── Session (authenticated by X-Exam-Token — no Keycloak needed) ─────────────
export const mandatoryExamSession = {
  validateToken: (token) =>
    axios.get(`/api/mandatory-exams/validate-token`, { params: { token } }),

  submit: (attemptId, data, examToken) =>
    axios.post(`/api/mandatory-exam-attempts/${attemptId}/submit`, data, {
      headers: { 'X-Exam-Token': examToken },
    }),

  getResult: (attemptId, examToken) =>
    axios.get(`/api/mandatory-exam-attempts/${attemptId}/result`, {
      headers: { 'X-Exam-Token': examToken },
    }),
}
