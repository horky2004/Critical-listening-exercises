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
      <div className="mb-8">
        <p className="text-sm font-medium text-accent">{strings.appTagline}</p>
        <h1 className="mt-1 text-3xl font-semibold tracking-tight">{strings.dashboard}</h1>
      </div>
      <QueryState isPending={modules.isPending} error={modules.error} onRetry={() => void modules.refetch()}>
        <div className="grid gap-5 md:grid-cols-2">
          {modules.data?.map((module) => {
            const body = (
              <Card
                className={`h-full transition duration-150 ${
                  module.isAvailable ? "hover:-translate-y-0.5 hover:border-accent/35" : "opacity-70"
                }`}
              >
                <div className="flex items-start justify-between gap-4">
                  <div>
                    <h2 className="text-xl font-semibold tracking-tight">{module.name}</h2>
                    <p className="mt-2 text-sm leading-6 text-muted">{module.description}</p>
                  </div>
                  {!module.isAvailable && (
                    <span className="rounded-full bg-panel-2 px-3 py-1 text-xs font-medium text-muted">
                      {strings.unavailable}
                    </span>
                  )}
                </div>
                <div className="mt-8 space-y-2">
                  <ProgressBar value={module.completedLevelCount} max={module.totalLevelCount} />
                  <p className="tabular text-sm text-muted">
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
