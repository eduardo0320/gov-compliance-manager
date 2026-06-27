export const DASHBOARD_CACHE_KEY = "dashboardArbol_v2";

export function invalidarCacheDashboard() {
  ["dashboardArbol_v2", "dashboardTreeData", "dashboardCacheTimestamp", "dashboardStats"]
    .forEach(k => localStorage.removeItem(k));
}
