import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import '@nexora/ui/styles.css';
import { App } from './app.js';

const root = document.querySelector('#root');
if (!root) throw new Error('Elemento raiz da aplicação não encontrado.');

createRoot(root).render(
  <StrictMode>
    <App />
  </StrictMode>,
);
