import api from './axios'

export const aiApi = {
  status:            ()     => api.get('/ai/status'),
  generateQuestions: (data) => api.post('/ai/generate-questions', data, { timeout: 60000 }),
}
