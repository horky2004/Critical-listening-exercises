import { Link, useParams } from "react-router-dom";
import { useAdminCohorts, useAdminStudentProgress, useUpdateStudent } from "../../api/hooks";
import { Card } from "../../components/Card";
import { QueryState } from "../../components/QueryState";
import { StatusIcon, statusLabel } from "../../components/StatusIcon";
import { strings } from "../../lib/strings";
import { AdminLayout } from "./AdminLayout";

export function AdminStudentProgressPage() {
  const { userId = "" } = useParams();
  const progress = useAdminStudentProgress(userId);
  const cohorts = useAdminCohorts();
  const update = useUpdateStudent();

  return (
    <AdminLayout>
      <Link to="/admin/students" className="mb-4 inline-block text-sm font-medium text-muted hover:text-accent">
        ← {strings.adminStudents}
      </Link>
      <QueryState isPending={progress.isPending} error={progress.error} onRetry={() => void progress.refetch()}>
        {progress.data && (
          <>
            <div className="mb-8 flex flex-wrap items-end justify-between gap-4">
              <div>
                <h2 className="text-2xl font-semibold tracking-tight">{progress.data.student.displayName}</h2>
                <p className="mt-1 text-sm text-muted">{progress.data.student.email}</p>
              </div>
              <label className="block text-sm">
                <span className="mb-1 block text-muted">{strings.adminAssignCohort}</span>
                <select
                  className="rounded-xl border border-line bg-panel-2 px-3 py-2"
                  disabled={update.isPending}
                  value={progress.data.student.cohortId ?? ""}
                  onChange={(event) =>
                    update.mutate({
                      userId,
                      cohortId: event.target.value || null
                    })
                  }
                >
                  <option value="">{strings.adminNoCohort}</option>
                  {cohorts.data?.map((cohort) => (
                    <option key={cohort.id} value={cohort.id}>
                      {cohort.name}
                    </option>
                  ))}
                </select>
              </label>
            </div>
            {update.error && <p className="mb-4 text-sm text-bad">{update.error.message}</p>}
            <div className="space-y-6">
              {progress.data.modules.map((module) => (
                <section key={module.slug}>
                  <h3 className="mb-3 text-lg font-semibold tracking-tight">{module.name}</h3>
                  <div className="grid gap-4 lg:grid-cols-2">
                    {module.sources.map((source) => (
                      <Card key={source.slug}>
                        <h4 className="font-semibold">{source.name}</h4>
                        <div className="mt-4 space-y-4">
                          {source.segments.map((segment) => (
                            <div key={segment.key}>
                              <p className="text-xs font-semibold uppercase tracking-[0.12em] text-muted">
                                {segment.name}
                              </p>
                              <ul className="mt-2 space-y-2">
                                {segment.levels.map((level) => (
                                  <li key={level.levelId} className="flex items-center justify-between gap-3 text-sm">
                                    <span className="flex items-center gap-2">
                                      <StatusIcon status={level.status} />
                                      {level.title}
                                    </span>
                                    <span className="tabular text-muted">
                                      {level.attemptCount > 0
                                        ? `${level.bestScore}/${level.questionCount} · ${statusLabel(level.status)}`
                                        : statusLabel(level.status)}
                                    </span>
                                  </li>
                                ))}
                              </ul>
                            </div>
                          ))}
                        </div>
                      </Card>
                    ))}
                  </div>
                </section>
              ))}
            </div>
          </>
        )}
      </QueryState>
    </AdminLayout>
  );
}
