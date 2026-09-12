import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { api } from "./client";
import type {
  AnswerView,
  MeResponse,
  ModuleListItem,
  PreviewResponse,
  SessionView,
  SourceListItem,
  TreeResponse
} from "./types";

export const queryKeys = {
  me: ["me"] as const,
  modules: ["modules"] as const,
  sources: (moduleSlug: string) => ["sources", moduleSlug] as const,
  tree: (moduleSlug: string, sourceSlug: string) => ["tree", moduleSlug, sourceSlug] as const,
  session: (sessionId: string) => ["session", sessionId] as const,
  preview: (moduleSlug: string, sourceSlug: string, levelId: string) =>
    ["preview", moduleSlug, sourceSlug, levelId] as const
};

export function useMe() {
  return useQuery({
    queryKey: queryKeys.me,
    queryFn: () => api.get<MeResponse>("/api/me")
  });
}

export function useModules() {
  return useQuery({
    queryKey: queryKeys.modules,
    queryFn: async () => (await api.get<{ modules: ModuleListItem[] }>("/api/modules")).modules
  });
}

export function useSources(moduleSlug: string) {
  return useQuery({
    queryKey: queryKeys.sources(moduleSlug),
    queryFn: async () => {
      const data = await api.get<{
        module: { slug: string; name: string };
        sources: SourceListItem[];
      }>(`/api/modules/${moduleSlug}/sources`);
      return data;
    }
  });
}

export function useTree(moduleSlug: string, sourceSlug: string) {
  return useQuery({
    queryKey: queryKeys.tree(moduleSlug, sourceSlug),
    queryFn: () => api.get<TreeResponse>(`/api/modules/${moduleSlug}/sources/${sourceSlug}/tree`)
  });
}

export function usePreview(moduleSlug: string, sourceSlug: string, levelId: string) {
  return useQuery({
    queryKey: queryKeys.preview(moduleSlug, sourceSlug, levelId),
    queryFn: () =>
      api.get<PreviewResponse>(
        `/api/modules/${moduleSlug}/sources/${sourceSlug}/levels/${levelId}/preview`
      ),
    enabled: Boolean(moduleSlug && sourceSlug && levelId)
  });
}

export function useSession(sessionId: string | undefined) {
  return useQuery({
    queryKey: queryKeys.session(sessionId ?? ""),
    queryFn: () => api.get<SessionView>(`/api/test-sessions/${sessionId}`),
    enabled: Boolean(sessionId)
  });
}

export function useStartSession() {
  return useMutation({
    mutationFn: (body: { moduleSlug: string; sourceSlug: string; levelId: string }) =>
      api.post<SessionView>("/api/test-sessions", body)
  });
}

export function useAnswer(sessionId: string) {
  return useMutation({
    mutationFn: (body: { index: number; answerKey: string }) =>
      api.post<AnswerView>(`/api/test-sessions/${sessionId}/questions/${body.index}/answer`, {
        answerKey: body.answerKey
      })
  });
}

export function useAbandon(sessionId: string) {
  return useMutation({
    mutationFn: () => api.post<void>(`/api/test-sessions/${sessionId}/abandon`)
  });
}

export function invalidateProgress(queryClient: ReturnType<typeof useQueryClient>, moduleSlug: string, sourceSlug: string) {
  void queryClient.invalidateQueries({ queryKey: queryKeys.tree(moduleSlug, sourceSlug) });
  void queryClient.invalidateQueries({ queryKey: queryKeys.sources(moduleSlug) });
  void queryClient.invalidateQueries({ queryKey: queryKeys.modules });
}
