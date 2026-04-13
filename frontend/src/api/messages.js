import api from './axios'

export const messagesApi = {
  getConversations: () => api.get('/messages/conversations'),
  getMessages: (conversationId) => api.get(`/messages/${conversationId}`),
  send: (payload) => api.post('/messages/send', payload),       // { recipientId, content }
  markRead: (conversationId) => api.post(`/messages/${conversationId}/read`),
  unreadCount: () => api.get('/messages/unread-count'),
  deleteMessage: (messageId) => api.delete(`/messages/msg/${messageId}`),
}
