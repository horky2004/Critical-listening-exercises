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
      <Link to="/" className="md:absolute md:left-10 md:top-25 text-sm font-medium text-muted transition hover:text-accent">
        ← {strings.dashboard}
      </Link>
      <QueryState isPending={sources.isPending} error={sources.error} onRetry={() => void sources.refetch()}>
        {moduleSlug === "eq" ? (
          <div className="mx-auto mt-4 max-w-3xl">
            <h1 className="text-3xl font-semibold tracking-tight text-center">{sources.data?.module.name}</h1>
            <div className="mt-4 space-y-3">
              {strings.eqModuleIntro.map((paragraph) => (
                <p key={paragraph} className="text-sm leading-6 text-muted">
                  {paragraph}
                </p>
              ))}
            </div>
          </div>
        ) : (
          <h1 className="mt-4 text-3xl font-semibold tracking-tight">{sources.data?.module.name}</h1>
        )}
        <div className={`mx-auto max-w-3xl ${moduleSlug === "eq" ? "mt-8" : "mt-4"}`}>
          <p className="mb-8 text-center text-sm text-muted">{strings.sources}</p>
          <div className="grid grid-cols-2 gap-5">
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
                <div key={source.slug} aria-disabled="true">
                  {body}
                </div>
              );
            })}
          </div>
        </div>
      </QueryState>
    </Shell>
  );
}
