const storageKey = "cll-dev-role";

export type DevRole = "Student" | "Admin";

export function getDevRole(): DevRole {
  return localStorage.getItem(storageKey) === "Admin" ? "Admin" : "Student";
}

export function setDevRole(role: DevRole) {
  localStorage.setItem(storageKey, role);
}

export function switchDevRole(role: DevRole) {
  setDevRole(role);
  window.location.assign(role === "Admin" ? "/admin" : "/");
}
