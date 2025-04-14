const express = require('express');
const { createProxyMiddleware } = require('http-proxy-middleware');

const app = express();

const SERVICES = {
  plays: process.env.SERVICES_PLAYS || 'http://localhost:3000',
  reservations: process.env.SERVICES_RESERVATIONS || 'http://localhost:5000',
  users: process.env.SERVICES_USERS || 'http://localhost:8080'
};

// Proxy for /web/plays → /plays
// with separate logging
const playsProxy = createProxyMiddleware({
  target: SERVICES.plays,
  changeOrigin: true,
  pathRewrite: (path) => `/plays${path}` // Keeping your exact implementation
});

app.use('/web/plays', (req, res, next) => {
  console.log(`[GATEWAY] ${req.method} ${req.originalUrl} → ${SERVICES.plays}/plays${req.originalUrl.replace('/web/plays', '')}`);
  playsProxy(req, res, next);
});

// Proxy for /web/reservations → /reservations
// with separate logging
const reservationsProxy = createProxyMiddleware({
  target: SERVICES.reservations,
  changeOrigin: true,
  pathRewrite: (path) => `/reservations${path}` // Keeping your exact implementation
});

app.use('/web/reservations', (req, res, next) => {
  console.log(`[GATEWAY] ${req.method} ${req.originalUrl} → ${SERVICES.reservations}/reservations${req.originalUrl.replace('/web/reservations', '')}`);
  reservationsProxy(req, res, next);
});

// Proxy for /web/users → /users
// with separate logging
const usersProxy = createProxyMiddleware({
  target: SERVICES.users,
  changeOrigin: true,
  pathRewrite: (path) => `/users${path}` // Consistent with your style
});

app.use('/web/users', (req, res, next) => {
  console.log(`[GATEWAY] ${req.method} ${req.originalUrl} → ${SERVICES.users}/users${req.originalUrl.replace('/web/users', '')}`);
  usersProxy(req, res, next);
});

const PORT = 4000;
app.listen(PORT, () => {
  console.log(`\n API Gateway running on http://localhost:${PORT}`);
  console.log(`http://localhost:${PORT}/web/plays → ${SERVICES.plays}/plays`);
  console.log(`http://localhost:${PORT}/web/reservations → ${SERVICES.reservations}/reservations`);
  console.log(`(TBD) http://localhost:${PORT}/web/users → ${SERVICES.users}/users`);
});
