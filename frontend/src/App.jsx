import { useEffect, Component } from 'react'
import { BrowserRouter, Routes, Route, Navigate, useNavigate } from 'react-router-dom'
import { AuthProvider } from './contexts/AuthContext'
import ProtectedRoute from './components/ProtectedRoute'
import Login from './pages/Login'
import Dashboard from './pages/Dashboard'

class ErrorBoundary extends Component {
  state = { error: null }

  static getDerivedStateFromError(error) {
    return { error }
  }

  componentDidCatch(error, info) {
    console.error('Uncaught render error:', error, info.componentStack)
  }

  render() {
    if (this.state.error) {
      return (
        <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', height: '100vh', gap: '12px', fontFamily: 'sans-serif' }}>
          <h1 style={{ fontSize: '20px', fontWeight: '600' }}>Something went wrong</h1>
          <p style={{ color: '#6B7280', fontSize: '14px' }}>{this.state.error.message}</p>
          <button
            onClick={() => this.setState({ error: null })}
            style={{ padding: '8px 16px', background: '#3B82F6', color: 'white', border: 'none', borderRadius: '6px', cursor: 'pointer' }}
          >
            Try again
          </button>
        </div>
      )
    }
    return this.props.children
  }
}

function NavigationHandler() {
  const navigate = useNavigate()
  useEffect(() => {
    const handler = () => navigate('/login', { replace: true })
    window.addEventListener('auth:sessionExpired', handler)
    return () => window.removeEventListener('auth:sessionExpired', handler)
  }, [navigate])
  return null
}

export default function App() {
  return (
    <ErrorBoundary>
      <AuthProvider>
        <BrowserRouter>
          <NavigationHandler />
          <Routes>
            <Route path="/login" element={<Login />} />
            <Route
              path="/dashboard"
              element={
                <ProtectedRoute>
                  <Dashboard />
                </ProtectedRoute>
              }
            />
            <Route path="*" element={<Navigate to="/dashboard" replace />} />
          </Routes>
        </BrowserRouter>
      </AuthProvider>
    </ErrorBoundary>
  )
}
