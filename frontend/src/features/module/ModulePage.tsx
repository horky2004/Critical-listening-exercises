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
      <Link to="/" className="text-sm text-muted hover:text-ink">
        ← {strings.dashboard}
      </Link>
      <QueryState isPending={sources.isPending} error={sources.error} onRetry={() => void sources.refetch()}>
        <h1 className="mt-4 text-2xl font-semibold">{sources.data?.module.name}</h1>
        <p className="mt-1 mb-6 text-sm text-muted">{strings.sources}</p>
        <div className="grid gap-4 md:grid-cols-2">
          {sources.data?.sources.map((source) => (
            <Link key={source.slug} to={`/modules/${moduleSlug}/sources/${source.slug}`}>
              <Card className="hover:border-accent/40">
                <h2 className="text-lg font-medium">{source.name}</h2>
                <div className="mt-4 space-y-2">
                  <ProgressBar value={source.completedLevelCount} max={source.levelCount} />
                  <p className="tabular text-xs text-muted">
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
