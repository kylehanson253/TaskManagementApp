import { useAuth } from '../contexts/AuthContext'

const PRIORITY_COLORS = {
  Low: '#6B7280',
  Medium: '#F59E0B',
  High: '#EF4444',
  Critical: '#7C3AED',
}

const STATUS_COLORS = {
  Todo: '#6B7280',
  InProgress: '#3B82F6',
  Completed: '#10B981',
  Cancelled: '#9CA3AF',
}

export default function TaskCard({ task, onComplete, onEdit, onDelete }) {
  const { isAdmin, user } = useAuth()
  const canEdit = isAdmin || task.createdByUserId === user?.id
  const isCompleted = task.statusLabel === 'Completed'

  const dueDateStr = task.dueDate
    ? new Date(task.dueDate).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })
    : null

  const isOverdue = task.dueDate && !isCompleted && new Date(task.dueDate) < new Date()

  return (
    <div className={`task-card ${isCompleted ? 'task-card--completed' : ''}`}>
      <div className="task-card__header">
        <div className="task-card__badges">
          <span className="badge" style={{ color: STATUS_COLORS[task.statusLabel], background: STATUS_COLORS[task.statusLabel] + '20' }}>
            {task.statusLabel}
          </span>
          <span className="badge" style={{ color: PRIORITY_COLORS[task.priorityLabel], background: PRIORITY_COLORS[task.priorityLabel] + '20' }}>
            {task.priorityLabel}
          </span>
        </div>
        <div className="task-card__actions">
          {!isCompleted && (
            <button className="btn-icon btn-icon--success" title="Mark complete" onClick={() => onComplete(task.id)}>✓</button>
          )}
          {canEdit && (
            <button className="btn-icon" title="Edit" onClick={() => onEdit(task)}>✎</button>
          )}
          {(isAdmin || task.createdByUserId === user?.id) && (
            <button className="btn-icon btn-icon--danger" title="Delete" onClick={() => onDelete(task.id)}>✕</button>
          )}
        </div>
      </div>

      <h3 className={`task-card__title ${isCompleted ? 'task-card__title--done' : ''}`}>
        {task.title}
      </h3>

      {task.description && (
        <p className="task-card__desc">{task.description}</p>
      )}

      <div className="task-card__meta">
        {task.assignedToName && (
          <span title="Assigned to">👤 {task.assignedToName}</span>
        )}
        {dueDateStr && (
          <span className={isOverdue ? 'overdue' : ''} title="Due date">
            📅 {dueDateStr}{isOverdue ? ' (overdue)' : ''}
          </span>
        )}
        <span title="Created by" className="task-card__creator">by {task.createdByName}</span>
      </div>
    </div>
  )
}
