import { useNavigate } from "react-router-dom";

export default function NotFound() {
  const navigate = useNavigate();
  return (
    <div className="notfound-page">
      <div className="notfound-icon">🔍</div>
      <h1 className="notfound-code">404</h1>
      <h2 className="notfound-title">Página no encontrada</h2>
      <p className="notfound-desc">La página que buscás no existe o fue movida.</p>
      <button className="btn btn-primary" onClick={() => navigate("/")}>
        <i className="fas fa-home notfound-btn-icon"></i>
        Volver al inicio
      </button>
    </div>
  );
}