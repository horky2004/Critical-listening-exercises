import { useState } from "react";
import { useAdminCohorts, useCreateCohort, useUpdateCohort } from "../../api/hooks";
import type { AdminCohortItem } from "../../api/types";
import { Button } from "../../components/Button";
import { Card } from "../../components/Card";
import { QueryState } from "../../components/QueryState";
import { strings } from "../../lib/strings";
import { AdminLayout } from "./AdminLayout";

export function AdminCohortsPage() {
  const cohorts = useAdminCohorts();
  const create = useCreateCohort();
  const update = useUpdateCohort();
  const [name, setName] = useState("");
  const [active, setActive] = useState(false);

  return (
    <AdminLayout>
      <QueryState isPending={cohorts.isPending} error={cohorts.error} onRetry={() => void cohorts.refetch()}>
        <Card className="mb-6">
          <h2 className="text-lg font-semibold tracking-tight">{strings.adminNewCohort}</h2>
          <form
            className="mt-4 flex flex-wrap items-end gap-3"
            onSubmit={(event) => {
              event.preventDefault();
              if (!name.trim()) {
                return;
              }
              create.mutate(
                { name: name.trim(), isActive: active },
                {
                  onSuccess: () => {
                    setName("");
                    setActive(false);
                  }
                }
              );
            }}
          >
            <label className="block text-sm">
              <span className="mb-1 block text-muted">{strings.adminCohortName}</span>
              <input
                value={name}
                onChange={(event) => setName(event.target.value)}
                className="rounded-xl border border-line bg-white px-3 py-2"
              />
            </label>
            <label className="flex items-center gap-2 pb-2 text-sm">
              <input type="checkbox" checked={active} onChange={(event) => setActive(event.target.checked)} />
              {strings.adminActive}
            </label>
            <Button type="submit" disabled={create.isPending || !name.trim()}>
              {strings.adminCreate}
            </Button>
          </form>
          {create.error && <p className="mt-3 text-sm text-bad">{create.error.message}</p>}
        </Card>
        <div className="space-y-4">
          {cohorts.data?.map((cohort) => (
            <CohortCard
              key={cohort.id}
              cohort={cohort}
              busy={update.isPending}
              onSave={(next) => update.mutate({ id: cohort.id, ...next })}
            />
          ))}
        </div>
        {update.error && <p className="mt-4 text-sm text-bad">{update.error.message}</p>}
      </QueryState>
    </AdminLayout>
  );
}

function CohortCard({
  cohort,
  busy,
  onSave
}: {
  cohort: AdminCohortItem;
  busy: boolean;
  onSave: (next: { name: string; isActive: boolean }) => void;
}) {
  const [name, setName] = useState(cohort.name);

  return (
    <Card>
      <div className="flex flex-wrap items-end justify-between gap-4">
        <label className="block text-sm">
          <span className="mb-1 block text-muted">{strings.adminCohortName}</span>
          <input
            value={name}
            onChange={(event) => setName(event.target.value)}
            className="rounded-xl border border-line bg-white px-3 py-2"
          />
        </label>
        <div className="flex flex-wrap items-center gap-3">
          <span
            className={`rounded-full px-3 py-1 text-xs font-semibold ${
              cohort.isActive ? "bg-accent/10 text-accent" : "bg-panel-2 text-muted"
            }`}
          >
            {cohort.isActive ? strings.adminActive : strings.adminInactive}
          </span>
          <span className="text-sm text-muted">
            {cohort.studentCount} {strings.adminStudents.toLowerCase()}
          </span>
          <Button
            variant="ghost"
            disabled={busy || name.trim() === cohort.name}
            onClick={() => onSave({ name: name.trim(), isActive: cohort.isActive })}
          >
            {strings.adminSave}
          </Button>
          {!cohort.isActive && (
            <Button
              disabled={busy}
              onClick={() => {
                if (!window.confirm(`${strings.adminSetActive}: ${cohort.name}?`)) {
                  return;
                }
                onSave({ name: name.trim() || cohort.name, isActive: true });
              }}
            >
              {strings.adminSetActive}
            </Button>
          )}
        </div>
      </div>
    </Card>
  );
}
