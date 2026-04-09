import { reactive } from 'vue'

const toasts = reactive([])
let nextId = 0

export function useToast() {
  function show(message, type = 'info', duration = 3500) {
    const id = ++nextId
    toasts.push({ id, message, type })
    setTimeout(() => remove(id), duration)
    return id
  }

  function remove(id) {
    const i = toasts.findIndex(t => t.id === id)
    if (i !== -1) toasts.splice(i, 1)
  }

  return {
    toasts,
    success: (msg, dur) => show(msg, 'success', dur),
    error:   (msg, dur) => show(msg, 'error', dur),
    info:    (msg, dur) => show(msg, 'info', dur),
    warning: (msg, dur) => show(msg, 'warning', dur),
    remove,
  }
}
