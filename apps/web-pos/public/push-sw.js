// Service Worker DEDICADO a Web Push (US-081 §3/§7/§10) — separado de branding-sw.js (que só
// cuida de cache de /v1/public/branding); cada um reage a um evento de navegador diferente e
// misturá-los complicaria o cache-busting de cada um (ver notifications/push-notifications.ts).

self.addEventListener('push', (event) => {
  let payload = { title: 'Notificação', body: '' };
  if (event.data) {
    try {
      payload = { ...payload, ...event.data.json() };
    } catch {
      payload = { ...payload, body: event.data.text() };
    }
  }

  const title = payload.title || 'Notificação';
  const options = {
    body: payload.body || '',
    tag: payload.alertId,
    // Evita empilhar N notificações do navegador para o mesmo alerta em atualizações consecutivas
    // (US-083 §10 — mesmo princípio do "não repetir som", aplicado ao push).
    renotify: false,
    data: { alertId: payload.alertId, severity: payload.severity },
  };

  event.waitUntil(self.registration.showNotification(title, options));
});

self.addEventListener('notificationclick', (event) => {
  event.notification.close();
  event.waitUntil(
    self.clients.matchAll({ type: 'window', includeUncontrolled: true }).then((clientList) => {
      for (const client of clientList) {
        if ('focus' in client) return client.focus();
      }
      if (self.clients.openWindow) return self.clients.openWindow('/');
      return undefined;
    }),
  );
});
