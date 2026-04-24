import api from './axios'

export const coursesApi = {
  getAll: (params) => api.get('/courses', { params }),
  getMy: () => api.get('/courses/my'),
  getById: (id) => api.get(`/courses/${id}`),
  getCategories: () => api.get('/courses/categories'),
  create: (data) => api.post('/courses', data),
  update: (id, data) => api.put(`/courses/${id}`, data),
  delete: (id) => api.delete(`/courses/${id}`),
  uploadThumbnail: (id, file) => {
    const form = new FormData()
    form.append('file', file)
    return api.post(`/courses/${id}/thumbnail`, form)
  },
  enroll: (id) => api.post(`/courses/${id}/enroll`),
  submitReview: (id, data) => api.post(`/courses/${id}/reviews`, data),
}

export const resourcesApi = {
  getAll: (courseId) => api.get(`/courses/${courseId}/resources`),
  upload: (courseId, file, meta = {}) => {
    const form = new FormData()
    form.append('file', file)
    if (meta.title) form.append('title', meta.title)
    if (meta.description) form.append('description', meta.description)
    if (meta.order != null) form.append('order', meta.order)
    return api.post(`/courses/${courseId}/resources`, form, { timeout: 60000 })
  },
  update: (courseId, id, data) => api.put(`/courses/${courseId}/resources/${id}`, data),
  delete: (courseId, id) => api.delete(`/courses/${courseId}/resources/${id}`),
  download: (courseId, id) => api.post(`/courses/${courseId}/resources/${id}/download`),
}

export const modulesApi = {
  getAll: (courseId) => api.get(`/courses/${courseId}/modules`),
  getById: (courseId, id) => api.get(`/courses/${courseId}/modules/${id}`),
  create: (courseId, data) => api.post(`/courses/${courseId}/modules`, data),
  update: (courseId, id, data) => api.put(`/courses/${courseId}/modules/${id}`, data),
  delete: (courseId, id) => api.delete(`/courses/${courseId}/modules/${id}`),
  reorder: (courseId, items) => api.post(`/courses/${courseId}/modules/reorder`, { items }),
  uploadAttachment: (courseId, moduleId, file) => {
    const form = new FormData()
    form.append('file', file)
    return api.post(`/courses/${courseId}/modules/${moduleId}/attachments`, form, { timeout: 300000 })
  },
  deleteAttachment: (courseId, moduleId, attId) =>
    api.delete(`/courses/${courseId}/modules/${moduleId}/attachments/${attId}`),
}
