import api from './axios'
import axios from 'axios'

// ── Management (requires Keycloak auth) ──────────────────────────────────────
export const mandatoryExamApi = {
  getAll:          ()              => api.get('/mandatory-exams'),
  getById:         (id)            => api.get(`/mandatory-exams/${id}`),
  create:          (data)          => api.post('/mandatory-exams', data),
  update:          (id, data)      => api.put(`/mandatory-exams/${id}`, data),
  toggleActive:    (id)            => api.patch(`/mandatory-exams/${id}/toggle-active`),
  delete:          (id)            => api.delete(`/mandatory-exams/${id}`),

  addQuestion:     (id, data)      => api.post(`/mandatory-exams/${id}/questions`, data),
  updateQuestion:  (examId, qId, data) => api.put(`/mandatory-exams/${examId}/questions/${qId}`, data),
  deleteQuestion:  (examId, qId)   => api.delete(`/mandatory-exams/${examId}/questions/${qId}`),
  reorderQuestions:(examId, items) => api.put(`/mandatory-exams/${examId}/questions/reorder`, { items }),

  assign:          (id, data)      => api.post(`/mandatory-exams/${id}/assign`, data),
  unassign:        (id, userId)    => api.delete(`/mandatory-exams/${id}/assign/${userId}`),
  getAssignments:  (id)            => api.get(`/mandatory-exams/${id}/assignments`),

  generateLink:        (id, data)  => api.post(`/mandatory-exams/${id}/generate-link`, data),
  generateAccessCode:  (id)        => api.post(`/mandatory-exams/${id}/generate-access-code`),
  revokePublicLink:    (id)        => api.delete(`/mandatory-exams/${id}/access-code`),
  getSessions:         (id)        => api.get(`/mandatory-exams/${id}/sessions`),
  revokeSession:       (sessionId) => api.post(`/mandatory-exams/sessions/${sessionId}/revoke`),
  getAttempts:         (id)        => api.get(`/mandatory-exams/${id}/attempts`),
  gradeEssay:          (attemptId, answerId, data) => api.patch(`/mandatory-exams/attempts/${attemptId}/answers/${answerId}/grade`, data),
  importQuestions:     (id, data)  => api.post(`/mandatory-exams/${id}/import-questions`, data),
  exportResults:       (id)        => api.get(`/mandatory-exams/${id}/export`, { responseType: 'blob' }),

  uploadCertTemplate:  (id, file)  => {
    const form = new FormData()
    form.append('file', file)
    return api.post(`/mandatory-exams/${id}/certificate-template`, form, {
      headers: { 'Content-Type': 'multipart/form-data' }
    })
  },
  getCertTemplate:     (id)        => api.get(`/mandatory-exams/${id}/certificate-template`),
  deleteCertTemplate:  (id)        => api.delete(`/mandatory-exams/${id}/certificate-template`),
  getIssuedCerts:      (id)        => api.get(`/mandatory-exams/${id}/certificates`),
}

// ── Session (authenticated by X-Exam-Token — no Keycloak needed) ─────────────
export const mandatoryExamSession = {
  validateToken: (token) =>
    axios.get(`/api/mandatory-exams/validate-token`, { params: { token } }),

  claimLink: (linkToken, userName) =>
    axios.post(`/api/mandatory-exams/claim-link`, { userName }, { params: { linkToken } }),

  accessByCode: (code, userId, userName) =>
    axios.get(`/api/mandatory-exams/access`, { params: { code, userId, userName } }),

  submit: (attemptId, data, examToken) =>
    axios.post(`/api/mandatory-exam-attempts/${attemptId}/submit`, data, {
      headers: { 'X-Exam-Token': examToken },
    }),

  getResult: (attemptId, examToken) =>
    axios.get(`/api/mandatory-exam-attempts/${attemptId}/result`, {
      headers: { 'X-Exam-Token': examToken },
    }),

  downloadCertificate: (certNumber, examToken) =>
    axios.get(`/api/mandatory-exams/certificates/${certNumber}/download`, {
      headers: { 'X-Exam-Token': examToken },
      responseType: 'blob',
    }),
}
