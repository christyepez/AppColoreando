const runtimeApi = (globalThis as { APP_COLOREANDO_API?: string }).APP_COLOREANDO_API;

const browserApi = typeof window !== 'undefined'
  ? `${window.location.protocol}//${window.location.hostname}:8086/api`
  : 'http://127.0.0.1:8086/api';

export const apiBase = runtimeApi ?? browserApi;
