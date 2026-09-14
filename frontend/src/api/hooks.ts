import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { api } from "./client";
import { hasFrequencyIntro } from "../features/intro/introFlow";
import type {
  AnswerView,
  AdminCohortItem,
  AdminModuleItem,
  AdminStudentRef,
  MeResponse,
  ModuleListItem,
  IntroResponse,
  PracticeResponse,
  PreviewResponse,
  SessionView,
  SourceListItem,
  StudentListResponse,
  StudentProgressResponse,
  TreeResponse,
  UpdateModuleResponse
} from "./types";

export const queryKeys = {
  me: ["me"] as const,
  modules: ["modules"] as const,
  sources: (moduleSlug: string) => ["sources", moduleSlug] as const,
  tree: (moduleSlug: string, sourceSlug: string) => ["tree", moduleSlug, sourceSlug] as const,
  session: (sessionId: string) => ["session", sessionId] as const,
  preview: (moduleSlug: string, sourceSlug: string, levelId: string) =>
    ["preview", moduleSlug, sourceSlug, levelId] as const,
  practice: (moduleSlug: string, sourceSlug: string) => ["practice", moduleSlug, sourceSlug] as const,
  intro: (moduleSlug: string, sourceSlug: string) => ["intro", moduleSlug, sourceSlug] as const,
  adminModules: ["admin", "modules"] as const,
  adminCohorts: ["admin", "cohorts"] as const,
  adminStudents: (cohortId: string, search: string, page: number) =>
    ["admin", "students", cohortId, search, page] as const,
  adminStudentProgress: (userId: string) => ["admin", "students", userId, "progress"] as const
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

export function useIntro(moduleSlug: string, sourceSlug: string) {
  return useQuery({
    queryKey: queryKeys.intro(moduleSlug, sourceSlug),
    queryFn: () => api.get<IntroResponse>(`/api/modules/${moduleSlug}/sources/${sourceSlug}/intro`),
    enabled: hasFrequencyIntro(moduleSlug, sourceSlug)
  });
}

export function usePractice(moduleSlug: string, sourceSlug: string) {
  return useQuery({
    queryKey: queryKeys.practice(moduleSlug, sourceSlug),
    queryFn: () =>
      api.get<PracticeResponse>(`/api/modules/${moduleSlug}/sources/${sourceSlug}/practice`),
    enabled: Boolean(moduleSlug && sourceSlug)
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
  void queryClient.invalidateQueries({ queryKey: queryKeys.intro(moduleSlug, sourceSlug) });
  void queryClient.invalidateQueries({ queryKey: queryKeys.sources(moduleSlug) });
  void queryClient.invalidateQueries({ queryKey: queryKeys.modules });
}

export function useAdminModules() {
  return useQuery({
    queryKey: queryKeys.adminModules,
    queryFn: async () => (await api.get<{ modules: AdminModuleItem[] }>("/api/admin/modules")).modules
  });
}

export function useUpdateModule() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (body: { slug: string; isEnabledGlobally: boolean }) =>
      api.put<UpdateModuleResponse>(`/api/admin/modules/${body.slug}`, {
        isEnabledGlobally: body.isEnabledGlobally
      }),
    onSuccess: () => void queryClient.invalidateQueries({ queryKey: queryKeys.adminModules })
  });
}

export function useAdminCohorts() {
  return useQuery({
    queryKey: queryKeys.adminCohorts,
    queryFn: async () => (await api.get<{ cohorts: AdminCohortItem[] }>("/api/admin/cohorts")).cohorts
  });
}

export function useCreateCohort() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (body: { name: string; isActive: boolean }) =>
      api.post<AdminCohortItem>("/api/admin/cohorts", body),
    onSuccess: () => void queryClient.invalidateQueries({ queryKey: queryKeys.adminCohorts })
  });
}

export function useUpdateCohort() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (body: { id: string; name: string; isActive: boolean }) =>
      api.put<AdminCohortItem>(`/api/admin/cohorts/${body.id}`, {
        name: body.name,
        isActive: body.isActive
      }),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.adminCohorts });
      void queryClient.invalidateQueries({ queryKey: queryKeys.adminModules });
    }
  });
}

export function useUpdateCohortModule() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (body: { cohortId: string; moduleSlug: string; isEnabled: boolean | null }) =>
      api.put<void>(`/api/admin/cohorts/${body.cohortId}/modules/${body.moduleSlug}`, {
        isEnabled: body.isEnabled
      }),
    onSuccess: () => void queryClient.invalidateQueries({ queryKey: queryKeys.adminModules })
  });
}

export function useAdminStudents(cohortId: string, search: string, page: number) {
  return useQuery({
    queryKey: queryKeys.adminStudents(cohortId, search, page),
    queryFn: () => {
      const params = new URLSearchParams({ page: String(page), pageSize: "50" });
      if (cohortId) {
        params.set("cohortId", cohortId);
      }
      if (search.trim()) {
        params.set("search", search.trim());
      }
      return api.get<StudentListResponse>(`/api/admin/students?${params}`);
    }
  });
}

export function useAdminStudentProgress(userId: string) {
  return useQuery({
    queryKey: queryKeys.adminStudentProgress(userId),
    queryFn: () => api.get<StudentProgressResponse>(`/api/admin/students/${userId}/progress`),
    enabled: Boolean(userId)
  });
}

export function useUpdateStudent() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (body: { userId: string; cohortId: string | null }) =>
      api.put<AdminStudentRef>(`/api/admin/students/${body.userId}`, { cohortId: body.cohortId }),
    onSuccess: (_data, body) => {
      void queryClient.invalidateQueries({ queryKey: ["admin", "students"] });
      void queryClient.invalidateQueries({ queryKey: queryKeys.adminStudentProgress(body.userId) });
      void queryClient.invalidateQueries({ queryKey: queryKeys.adminCohorts });
    }
  });
}
