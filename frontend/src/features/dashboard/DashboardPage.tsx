import { Link } from "react-router-dom";
import { useModules } from "../../api/hooks";
import { Card } from "../../components/Card";
import { ProgressBar } from "../../components/ProgressBar";
import { QueryState } from "../../components/QueryState";
import { Shell } from "../../components/Shell";
import { strings } from "../../lib/strings";

export function DashboardPage() {
  const modules = useModules();

  return (
    <Shell>
      <h1 className="mb-6 text-2xl font-semibold">{strings.dashboard}</h1>
      <QueryState isPending={modules.isPending} error={modules.error} onRetry={() => void modules.refetch()}>
        <div className="grid gap-4 md:grid-cols-2">
          {modules.data?.map((module) => {
            const body = (
              <Card className={module.isAvailable ? "hover:border-accent/40" : "opacity-70"}>
                <div className="flex items-start justify-between gap-4">
                  <div>
                    <h2 className="text-lg font-medium">{module.name}</h2>
                    <p className="mt-1 text-sm text-muted">{module.description}</p>
                  </div>
                  {!module.isAvailable && (
                    <span className="rounded border border-line px-2 py-1 text-xs text-muted">
                      {strings.unavailable}
                    </span>
                  )}
                </div>
                <div className="mt-5 space-y-2">
                  <ProgressBar value={module.completedLevelCount} max={module.totalLevelCount} />
                  <p className="tabular text-xs text-muted">
                    {module.completedLevelCount}/{module.totalLevelCount} · {module.sourceCount} {strings.sources.toLowerCase()}
                  </p>
                </div>
                {!module.isAvailable && (
                  <p className="mt-3 text-xs text-muted">
                    {module.unavailableReason === "NotEnabledForCohort"
                      ? strings.unavailableCohort
                      : strings.unavailableGlobally}
                  </p>
                )}
              </Card>
            );

            return module.isAvailable ? (
              <Link key={module.slug} to={`/modules/${module.slug}`}>
                {body}
              </Link>
            ) : (
              <div key={module.slug}>{body}</div>
            );
          })}
        </div>
      </QueryState>
    </Shell>
  );
}
