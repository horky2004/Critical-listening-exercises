import { Link, useParams } from "react-router-dom";
import { useIntro, useTree } from "../../api/hooks";
import { Button } from "../../components/Button";
import { QueryState } from "../../components/QueryState";
import { Shell } from "../../components/Shell";
import { strings } from "../../lib/strings";
import { introCta } from "../intro/introFlow";
import { ProgressionTree } from "./ProgressionTree";

export function SourcePage() {
  const { moduleSlug = "", sourceSlug = "" } = useParams();
  const tree = useTree(moduleSlug, sourceSlug);
  const intro = useIntro(moduleSlug, sourceSlug);
  const cta = intro.data ? introCta(intro.data) : null;

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
            {cta && (
              <div className="mb-8 rounded-3xl border border-accent/20 bg-panel p-6 shadow-[0_16px_40px_rgba(21,32,51,0.06)]">
                <p className="text-sm font-medium text-accent">{strings.introTitle}</p>
                <p className="mt-2 text-lg font-semibold tracking-tight">
                  {cta === "continue-b" ? strings.introContinue : strings.introStart}
                </p>
                <p className="mt-2 w-full text-sm text-muted">
                  {cta === "continue-b" ? strings.introLeadB : strings.introLead}
                </p>
                <Link to={`/modules/${moduleSlug}/sources/${sourceSlug}/intro`} className="mt-5 block">
                  <Button className="w-full py-3.5 text-base">
                    {cta === "continue-b" ? strings.continueIntro : strings.introStart}
                  </Button>
                </Link>
              </div>
            )}
            <ProgressionTree tree={tree.data} moduleSlug={moduleSlug} sourceSlug={sourceSlug} />
          </>
        )}
      </QueryState>
    </Shell>
  );
}
