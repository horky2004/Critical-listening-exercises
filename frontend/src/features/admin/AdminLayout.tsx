import type { ReactNode } from "react";
import { NavLink } from "react-router-dom";
import { Shell } from "../../components/Shell";
import { strings } from "../../lib/strings";

const links = [
  { to: "/admin/modules", label: strings.adminModules },
  { to: "/admin/cohorts", label: strings.adminCohorts },
  { to: "/admin/students", label: strings.adminStudents }
];

export function AdminLayout({ children }: { children: ReactNode }) {
  return (
    <Shell>
      <div className="mx-auto max-w-6xl">
        <h1 className="text-3xl font-semibold tracking-tight">{strings.admin}</h1>
        <nav className="mt-6 flex flex-wrap gap-2 border-b border-line pb-px">
          {links.map((link) => (
            <NavLink
              key={link.to}
              to={link.to}
              className={({ isActive }) =>
                `rounded-t-xl px-4 py-2.5 text-sm font-semibold transition ${
                  isActive
                    ? "border border-b-bg border-line bg-panel text-ink"
                    : "text-muted hover:text-ink"
                }`
              }
            >
              {link.label}
            </NavLink>
          ))}
        </nav>
        <div className="mt-8">{children}</div>
      </div>
    </Shell>
  );
}
