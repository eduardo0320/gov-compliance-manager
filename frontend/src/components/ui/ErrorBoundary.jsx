import React from "react";

export default class ErrorBoundary extends React.Component {
  constructor(props) {
    super(props);
    this.state = { hasError: false, error: null };
  }

  static getDerivedStateFromError(error) {
    return { hasError: true, error };
  }

  componentDidCatch(error, info) {
    // En producción podrías enviar esto a un servicio de monitoreo
    console.error("[ErrorBoundary]", error, info.componentStack);
  }

  render() {
    if (this.state.hasError) {
      return (
        <div className="error-boundary-page">
          <div className="error-boundary-icon">⚠️</div>
          <h2 className="error-boundary-title">Algo salió mal</h2>
          <p className="error-boundary-desc">Ocurrió un error inesperado en esta sección.</p>
          <button
            className="btn btn-primary"
            onClick={() => this.setState({ hasError: false, error: null })}
          >
            Reintentar
          </button>
          <button
            className="btn btn-secondary"
className="ms-2"
            onClick={() => window.location.href = "/"}
          >
            Volver al inicio
          </button>
        </div>
      );
    }
    return this.props.children;
  }
}