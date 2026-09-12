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
      <Link to={`/modules/${moduleSlug}`} className="text-sm text-muted hover:text-ink">
        ← {strings.sources}
      </Link>
      <QueryState isPending={tree.isPending} error={tree.error} onRetry={() => void tree.refetch()}>
        {tree.data && (
          <>
            <div className="mt-4 mb-6 flex flex-wrap items-end justify-between gap-4">
              <div>
                <p className="text-sm text-muted">{tree.data.module.name}</p>
                <h1 className="text-2xl font-semibold">{tree.data.source.name}</h1>
              </div>
              <Button variant="ghost" disabled title={strings.practiceSoon}>
                {strings.practice}
              </Button>
            </div>
            <ProgressionTree tree={tree.data} moduleSlug={moduleSlug} sourceSlug={sourceSlug} />
          </>
        )}
      </QueryState>
    </Shell>
  );
}
