import { Link, useParams } from "react-router-dom";
import { useSources } from "../../api/hooks";
import { Card } from "../../components/Card";
import { ProgressBar } from "../../components/ProgressBar";
import { QueryState } from "../../components/QueryState";
import { Shell } from "../../components/Shell";
import { strings } from "../../lib/strings";

export function ModulePage() {
  const { moduleSlug = "" } = useParams();
  const sources = useSources(moduleSlug);

  return (
    <Shell>
      <Link to="/" className="text-sm font-medium text-muted transition hover:text-accent">
        ← {strings.dashboard}
      </Link>
      <QueryState isPending={sources.isPending} error={sources.error} onRetry={() => void sources.refetch()}>
        <h1 className="mt-4 text-3xl font-semibold tracking-tight">{sources.data?.module.name}</h1>
        <p className="mt-1 mb-8 text-sm text-muted">{strings.sources}</p>
        <div className="grid gap-5 sm:grid-cols-2 xl:grid-cols-4">
          {sources.data?.sources.map((source) => {
            const body = (
              <Card
                className={`h-full transition duration-150 ${
                  source.isAvailable ? "hover:-translate-y-0.5 hover:border-accent/35" : "opacity-70"
                }`}
              >
                <div className="flex items-start justify-between gap-3">
                  <h2 className="text-lg font-semibold tracking-tight">{source.name}</h2>
                  {!source.isAvailable && (
                    <span className="rounded-full bg-panel-2 px-3 py-1 text-xs font-medium text-muted">
                      {strings.sourceLocked}
                    </span>
                  )}
                </div>
                <div className="mt-6 space-y-2">
                  <ProgressBar value={source.completedLevelCount} max={source.levelCount} />
                  <p className="tabular text-sm text-muted">
                    {source.completedLevelCount}/{source.levelCount} savladano
                  </p>
                </div>
                {!source.isAvailable && (
                  <p className="mt-3 text-xs text-muted">{strings.sourceLockedHint}</p>
                )}
              </Card>
            );

            return source.isAvailable ? (
              <Link key={source.slug} to={`/modules/${moduleSlug}/sources/${source.slug}`}>
                {body}
              </Link>
            ) : (
              <div key={source.slug}>{body}</div>
            );
          })}
        </div>
      </QueryState>
    </Shell>
  );
}
