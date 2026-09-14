import { useAdminCohorts, useAdminModules, useUpdateCohortModule, useUpdateModule } from "../../api/hooks";
import type { AdminCohortItem, AdminModuleItem } from "../../api/types";
import { Button } from "../../components/Button";
import { Card } from "../../components/Card";
import { QueryState } from "../../components/QueryState";
import { strings } from "../../lib/strings";
import { AdminLayout } from "./AdminLayout";

export function AdminModulesPage() {
  const modules = useAdminModules();
  const cohorts = useAdminCohorts();
  const updateModule = useUpdateModule();
  const updateOverride = useUpdateCohortModule();

  return (
    <AdminLayout>
      <QueryState
        isPending={modules.isPending || cohorts.isPending}
        error={modules.error ?? cohorts.error}
        onRetry={() => {
          void modules.refetch();
          void cohorts.refetch();
        }}
      >
        <div className="space-y-5">
          {modules.data?.map((module) => (
            <ModuleCard
              key={module.slug}
              module={module}
              cohorts={cohorts.data ?? []}
              busy={updateModule.isPending || updateOverride.isPending}
              onToggleGlobal={async (enabled) => {
                const confirmText = enabled ? strings.adminEnableModuleConfirm : strings.adminDisableModuleConfirm;
                if (!window.confirm(confirmText)) {
                  return;
                }
                const result = await updateModule.mutateAsync({
                  slug: module.slug,
                  isEnabledGlobally: enabled
                });
                window.alert(`${strings.adminAffected}: ${result.affectedStudentCount}`);
              }}
              onOverride={(cohortId, isEnabled) =>
                updateOverride.mutate({ cohortId, moduleSlug: module.slug, isEnabled })
              }
            />
          ))}
        </div>
        {(updateModule.error || updateOverride.error) && (
          <p className="mt-4 text-sm text-bad">
            {updateModule.error?.message ?? updateOverride.error?.message}
          </p>
        )}
      </QueryState>
    </AdminLayout>
  );
}

function ModuleCard({
  module,
  cohorts,
  busy,
  onToggleGlobal,
  onOverride
}: {
  module: AdminModuleItem;
  cohorts: AdminCohortItem[];
  busy: boolean;
  onToggleGlobal: (enabled: boolean) => void;
  onOverride: (cohortId: string, isEnabled: boolean | null) => void;
}) {
  return (
    <Card>
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div>
          <h2 className="text-xl font-semibold tracking-tight">{module.name}</h2>
          <p className="mt-1 text-sm text-muted">
            {strings.adminGlobal}: {module.isEnabledGlobally ? strings.adminEnabled : strings.adminDisabled}
          </p>
        </div>
        <Button
          variant={module.isEnabledGlobally ? "danger" : "primary"}
          disabled={busy}
          onClick={() => onToggleGlobal(!module.isEnabledGlobally)}
        >
          {module.isEnabledGlobally ? strings.adminTurnOff : strings.adminTurnOn}
        </Button>
      </div>
      <table className="mt-6 w-full border-collapse text-sm">
        <thead>
          <tr className="border-b border-line text-left text-xs font-semibold uppercase tracking-[0.12em] text-muted">
            <th className="py-2 pr-3 font-semibold">{strings.adminCohorts}</th>
            <th className="py-2 font-semibold">{strings.adminAvailability}</th>
          </tr>
        </thead>
        <tbody>
          {cohorts.map((cohort) => {
            const override = module.cohortOverrides.find((item) => item.cohortId === cohort.id);
            const value = override ? (override.isEnabled ? "on" : "off") : "inherit";
            return (
              <tr key={cohort.id} className="border-b border-line/70 last:border-b-0">
                <td className="py-3 pr-3">
                  {cohort.name}
                  {cohort.isActive ? (
                    <span className="ml-2 rounded-full bg-accent/10 px-2 py-0.5 text-xs font-medium text-accent">
                      {strings.adminActive}
                    </span>
                  ) : null}
                </td>
                <td className="py-3">
                  <select
                    className="rounded-lg border border-line bg-panel-2 px-3 py-1.5 text-sm"
                    disabled={busy}
                    value={value}
                    onChange={(event) => {
                      const next = event.target.value;
                      onOverride(cohort.id, next === "inherit" ? null : next === "on");
                    }}
                  >
                    <option value="inherit">
                      {strings.adminInherit} (
                      {module.isEnabledGlobally ? strings.adminEnabled : strings.adminDisabled})
                    </option>
                    <option value="on">{strings.adminEnabled}</option>
                    <option value="off">{strings.adminDisabled}</option>
                  </select>
                </td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </Card>
  );
}
