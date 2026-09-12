import { Link, useParams } from "react-router-dom";
import { useTree } from "../../api/hooks";
import { Button } from "../../components/Button";
import { QueryState } from "../../components/QueryState";
import { Shell } from "../../components/Shell";
import { strings } from "../../lib/strings";
import { ProgressionTree } from "./ProgressionTree";

export function SourcePage() {
  const { moduleSlug = "", sourceSlug = "" } = useParams();
  const tree = useTree(moduleSlug, sourceSlug);

  return (
    <Shell>
      <Link to={`/modules/${moduleSlug}`} className="text-sm font-medium text-muted transition hover:text-accent">
        ← {strings.sources}
      </Link>
      <QueryState isPending={tree.isPending} error={tree.error} onRetry={() => void tree.refetch()}>
        {tree.data && (
          <>
            <div className="mt-4 mb-8 flex flex-wrap items-end justify-between gap-4">
              <div>
                <p className="text-sm font-medium text-accent">{tree.data.module.name}</p>
                <h1 className="text-3xl font-semibold tracking-tight">{tree.data.source.name}</h1>
              </div>
              {moduleSlug === "eq" ? (
                <Link to={`/modules/${moduleSlug}/sources/${sourceSlug}/practice`}>
                  <Button variant="ghost">{strings.practice}</Button>
                </Link>
              ) : (
                <Button variant="ghost" disabled title={strings.practiceSoon}>
                  {strings.practice}
                </Button>
              )}
            </div>
            <ProgressionTree tree={tree.data} moduleSlug={moduleSlug} sourceSlug={sourceSlug} />
          </>
        )}
      </QueryState>
    </Shell>
  );
}
