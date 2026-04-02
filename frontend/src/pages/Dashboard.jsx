import { useState, useEffect, useCallback, useMemo } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '../contexts/AuthContext'
import { tasksApi } from '../services/api'
import TaskCard from '../components/TaskCard'
import TaskModal from '../components/TaskModal'

const FILTER_OPTIONS = [
  { label: 'All', value: 'all' },
  { label: 'To Do', value: 'Todo' },
  { label: 'In Progress', value: 'InProgress' },
  { label: 'Completed', value: 'Completed' },
]

export default function Dashboard() {
  const { user, logout, isAdmin } = useAuth()
  const navigate = useNavigate()

  const [tasks, setTasks] = useState([])
  const [filter, setFilter] = useState('all')
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [editingTask, setEditingTask] = useState(null)
  const [showNewModal, setShowNewModal] = useState(false)

  const summary = useMemo(() => ({
    totalTasks:      tasks.length,
    todoCount:       tasks.filter(t => t.statusLabel === 'Todo').length,
    inProgressCount: tasks.filter(t => t.statusLabel === 'InProgress').length,
    completedCount:  tasks.filter(t => t.statusLabel === 'Completed').length,
  }), [tasks])

  const loadData = useCallback(async () => {
    setLoading(true)
    setError('')
    try {
      const { data } = await tasksApi.getAll()
      setTasks(data)
    } catch (err) {
      setError('Failed to load tasks.')
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => { loadData() }, [loadData])

  const handleLogout = useCallback(async () => {
    await logout()
    navigate('/login')
  }, [logout, navigate])

  const handleComplete = useCallback(async (id) => {
    try {
      const { data } = await tasksApi.complete(id)
      setTasks(prev => prev.map(t => t.id === id ? data : t))
    } catch (err) {
      alert(err.response?.data?.error ?? 'Failed to complete task.')
    }
  }, [])

  const handleDelete = useCallback(async (id) => {
    if (!confirm('Delete this task?')) return
    try {
      await tasksApi.delete(id)
      setTasks(prev => prev.filter(t => t.id !== id))
    } catch (err) {
      alert(err.response?.data?.error ?? 'Failed to delete task.')
    }
  }, [])

  const handleSaveNew = useCallback(async (payload) => {
    const { data } = await tasksApi.create(payload)
    setTasks(prev => [data, ...prev])
  }, [])

  const handleSaveEdit = useCallback(async (payload) => {
    const { data } = await tasksApi.update(editingTask.id, payload)
    setTasks(prev => prev.map(t => t.id === data.id ? data : t))
  }, [editingTask])

  const filteredTasks = filter === 'all'
    ? tasks
    : tasks.filter(t => t.statusLabel === filter)

  return (
    <div className="app-layout">
      {/* Sidebar */}
      <aside className="sidebar">
        <div className="sidebar__brand">
          <span className="logo-icon">✓</span>
          <span>Task Manager</span>
        </div>

        <nav className="sidebar__nav">
          {FILTER_OPTIONS.map(opt => (
            <button
              key={opt.value}
              className={`sidebar__nav-item ${filter === opt.value ? 'active' : ''}`}
              onClick={() => setFilter(opt.value)}
            >
              {opt.label}
              {summary && opt.value !== 'all' && (
                <span className="nav-count">
                  {opt.value === 'Todo' ? summary.todoCount
                   : opt.value === 'InProgress' ? summary.inProgressCount
                   : summary.completedCount}
                </span>
              )}
              {opt.value === 'all' && summary && (
                <span className="nav-count">{summary.totalTasks}</span>
              )}
            </button>
          ))}
        </nav>

        <div className="sidebar__footer">
          <div className="sidebar__user">
            <div className="avatar">{user?.firstName?.[0]}{user?.lastName?.[0]}</div>
            <div>
              <p className="user-name">{user?.firstName} {user?.lastName}</p>
              <p className="user-role">{user?.roleLabel} · {user?.tenantName}</p>
            </div>
          </div>
          <button className="btn btn-secondary btn--full" onClick={handleLogout}>Sign Out</button>
        </div>
      </aside>

      {/* Main */}
      <main className="main-content">
        <header className="main-header">
          <div>
            <h1 className="page-title">
              {filter === 'all' ? 'All Tasks' : FILTER_OPTIONS.find(o => o.value === filter)?.label}
            </h1>
            <p className="page-subtitle">{user?.tenantName}</p>
          </div>
          <button className="btn btn-primary" onClick={() => setShowNewModal(true)}>
            + New Task
          </button>
        </header>

        {/* Summary stats */}
        {!loading && (
          <div className="stats-grid">
            <div className="stat-card">
              <span className="stat-value">{summary.totalTasks}</span>
              <span className="stat-label">Total</span>
            </div>
            <div className="stat-card stat-card--yellow">
              <span className="stat-value">{summary.todoCount}</span>
              <span className="stat-label">To Do</span>
            </div>
            <div className="stat-card stat-card--blue">
              <span className="stat-value">{summary.inProgressCount}</span>
              <span className="stat-label">In Progress</span>
            </div>
            <div className="stat-card stat-card--green">
              <span className="stat-value">{summary.completedCount}</span>
              <span className="stat-label">Completed</span>
            </div>
          </div>
        )}

        {/* Task list */}
        {loading ? (
          <div className="loading-state">Loading tasks…</div>
        ) : error ? (
          <div className="alert alert--error">{error}</div>
        ) : filteredTasks.length === 0 ? (
          <div className="empty-state">
            <p>No tasks here. <button className="link-btn" onClick={() => setShowNewModal(true)}>Create one?</button></p>
          </div>
        ) : (
          <div className="task-grid">
            {filteredTasks.map(task => (
              <TaskCard
                key={task.id}
                task={task}
                onComplete={handleComplete}
                onEdit={(t) => setEditingTask(t)}
                onDelete={handleDelete}
              />
            ))}
          </div>
        )}
      </main>

      {/* Modals */}
      {showNewModal && (
        <TaskModal
          onClose={() => setShowNewModal(false)}
          onSave={handleSaveNew}
        />
      )}
      {editingTask && (
        <TaskModal
          task={editingTask}
          onClose={() => setEditingTask(null)}
          onSave={handleSaveEdit}
        />
      )}
    </div>
  )
}
