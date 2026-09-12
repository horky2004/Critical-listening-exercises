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
          {sources.data?.sources.map((source) => (
            <Link key={source.slug} to={`/modules/${moduleSlug}/sources/${source.slug}`}>
              <Card className="h-full transition duration-150 hover:-translate-y-0.5 hover:border-accent/35">
                <h2 className="text-lg font-semibold tracking-tight">{source.name}</h2>
                <div className="mt-6 space-y-2">
                  <ProgressBar value={source.completedLevelCount} max={source.levelCount} />
                  <p className="tabular text-sm text-muted">
                    {source.completedLevelCount}/{source.levelCount}
                  </p>
                </div>
              </Card>
            </Link>
          ))}
        </div>
      </QueryState>
    </Shell>
  );
}
