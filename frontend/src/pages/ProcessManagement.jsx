import React from 'react';
import { useParams } from 'react-router-dom';
import ProcessDomain from '../components/ui/ProcessDomain';
import ProcessDetail from '../components/ui/ProcessDetail';
import ProcessList from '../components/ui/ProcessList';

// Este componente actúa como router visual:
//   /processes                          → ProcessList (lista todos los dominios)
//   /processes/:dominioId               → ProcessDomain (procesos del dominio)
//   /processes/:dominioId/:procesoId    → ProcessDetail (subdominios del proceso)
const ProcessManagement = () => {
  const { dominioId, procesoId } = useParams();

  if (dominioId && procesoId) {
    return <ProcessDetail />;
  }

  if (dominioId) {
    return <ProcessDomain />;
  }

  return <ProcessList />;
};

export default ProcessManagement;
