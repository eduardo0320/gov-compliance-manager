import { Navigate } from "react-router-dom";
import { useAuth } from "../context/AuthContext";

export function AdminRoute({ children }) {
  const { isAdmin } = useAuth();
  return isAdmin ? children : <Navigate to="/" replace />;
}

export function EditorRoute({ children }) {
  const { isAdmin, isEditor } = useAuth();
  return (isAdmin || isEditor) ? children : <Navigate to="/" replace />;
}
