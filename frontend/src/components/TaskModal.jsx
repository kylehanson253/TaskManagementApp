import { useState, useEffect, useRef } from 'react'
import { usersApi } from '../services/api'
import { useAuth } from '../contexts/AuthContext'

const FOCUSABLE_SELECTORS =
  'button:not([disabled]), [href], input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])'

const PRIORITIES = ['Low', 'Medium', 'High', 'Critical']
const STATUSES = ['Todo', 'InProgress', 'Completed', 'Cancelled']

export default function TaskModal({ task, onClose, onSave }) {
  const { isAdmin } = useAuth()
  const [users, setUsers] = useState([])
  const [form, setForm] = useState({
    title: task?.title ?? '',
    description: task?.description ?? '',
    priority: task?.priority ?? 1,
    status: task?.status ?? 0,
    assignedToUserId: task?.assignedToUserId ?? '',
    dueDate: task?.dueDate ? task.dueDate.split('T')[0] : '',
  })
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const modalRef = useRef(null)

  useEffect(() => {
    if (isAdmin) {
      usersApi.getAll()
        .then(r => setUsers(r.data))
        .catch(() => {})
    }
  }, [isAdmin])

  // Escape to close + focus trap
  useEffect(() => {
    const previouslyFocused = document.activeElement
    const focusable = () => [...modalRef.current?.querySelectorAll(FOCUSABLE_SELECTORS) ?? []]

    // Focus first element on open
    focusable()[0]?.focus()

    const handleKeyDown = (e) => {
      if (e.key === 'Escape') {
        onClose()
        return
      }
      if (e.key === 'Tab') {
        const els = focusable()
        if (!els.length) { e.preventDefault(); return }
        const first = els[0]
        const last  = els[els.length - 1]
        if (e.shiftKey && document.activeElement === first) {
          e.preventDefault()
          last.focus()
        } else if (!e.shiftKey && document.activeElement === last) {
          e.preventDefault()
          first.focus()
        }
      }
    }

    document.addEventListener('keydown', handleKeyDown)
    return () => {
      document.removeEventListener('keydown', handleKeyDown)
      previouslyFocused?.focus()  // restore focus to trigger element on close
    }
  }, [onClose])

  const handleChange = (e) => {
    const { name, value } = e.target
    setForm(prev => ({ ...prev, [name]: value }))
  }

  const handleSubmit = async (e) => {
    e.preventDefault()
    setError('')
    setSaving(true)
    try {
      const payload = {
        title: form.title,
        description: form.description || null,
        priority: parseInt(form.priority),
        status: parseInt(form.status),
        assignedToUserId: form.assignedToUserId ? parseInt(form.assignedToUserId) : null,
        dueDate: form.dueDate || null,
      }
      await onSave(payload)
      onClose()
    } catch (err) {
      setError(err.response?.data?.error ?? 'Failed to save task.')
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal" role="dialog" aria-modal="true" aria-label={task ? 'Edit Task' : 'New Task'} ref={modalRef} onClick={e => e.stopPropagation()}>
        <div className="modal-header">
          <h2>{task ? 'Edit Task' : 'New Task'}</h2>
          <button className="btn-icon" onClick={onClose}>✕</button>
        </div>

        <form onSubmit={handleSubmit}>
          <div className="form-group">
            <label>Title *</label>
            <input name="title" value={form.title} onChange={handleChange}
                   required maxLength={200} placeholder="Task title"/>
          </div>

          <div className="form-group">
            <label>Description</label>
            <textarea name="description" value={form.description} onChange={handleChange}
                      rows={3} maxLength={2000} placeholder="Optional description"/>
          </div>

          <div className="form-row">
            <div className="form-group">
              <label>Priority</label>
              <select name="priority" value={form.priority} onChange={handleChange}>
                {PRIORITIES.map((p, i) => <option key={p} value={i}>{p}</option>)}
              </select>
            </div>

            {task && (
              <div className="form-group">
                <label>Status</label>
                <select name="status" value={form.status} onChange={handleChange}>
                  {STATUSES.map((s, i) => <option key={s} value={i}>{s}</option>)}
                </select>
              </div>
            )}
          </div>

          <div className="form-row">
            {isAdmin && (
              <div className="form-group">
                <label>Assign To</label>
                <select name="assignedToUserId" value={form.assignedToUserId} onChange={handleChange}>
                  <option value="">Unassigned</option>
                  {users.map(u => (
                    <option key={u.id} value={u.id}>{u.firstName} {u.lastName}</option>
                  ))}
                </select>
              </div>
            )}

            <div className="form-group">
              <label>Due Date</label>
              <input type="date" name="dueDate" value={form.dueDate} onChange={handleChange}/>
            </div>
          </div>

          {error && <p className="error-text">{error}</p>}

          <div className="modal-footer">
            <button type="button" className="btn btn-secondary" onClick={onClose}>Cancel</button>
            <button type="submit" className="btn btn-primary" disabled={saving}>
              {saving ? 'Saving…' : 'Save Task'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
