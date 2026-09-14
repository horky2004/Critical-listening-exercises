import { useState } from "react";
import { Link } from "react-router-dom";
import { useAdminCohorts, useAdminStudents } from "../../api/hooks";
import { Button } from "../../components/Button";
import { Card } from "../../components/Card";
import { QueryState } from "../../components/QueryState";
import { formatDateTime } from "../../lib/format";
import { strings } from "../../lib/strings";
import { AdminLayout } from "./AdminLayout";

export function AdminStudentsPage() {
  const [cohortId, setCohortId] = useState("");
  const [search, setSearch] = useState("");
  const [appliedSearch, setAppliedSearch] = useState("");
  const [page, setPage] = useState(1);
  const cohorts = useAdminCohorts();
  const students = useAdminStudents(cohortId, appliedSearch, page);
  const totalPages = Math.max(1, Math.ceil((students.data?.totalCount ?? 0) / (students.data?.pageSize ?? 50)));

  return (
    <AdminLayout>
      <form
        className="mb-6 flex flex-wrap items-end gap-3"
        onSubmit={(event) => {
          event.preventDefault();
          setPage(1);
          setAppliedSearch(search);
        }}
      >
        <label className="block text-sm">
          <span className="mb-1 block text-muted">{strings.adminSearchStudents}</span>
          <input
            value={search}
            onChange={(event) => setSearch(event.target.value)}
            className="rounded-xl border border-line bg-white px-3 py-2"
          />
        </label>
        <label className="block text-sm">
          <span className="mb-1 block text-muted">{strings.adminCohorts}</span>
          <select
            value={cohortId}
            onChange={(event) => {
              setCohortId(event.target.value);
              setPage(1);
            }}
            className="rounded-xl border border-line bg-white px-3 py-2"
          >
            <option value="">{strings.adminAllCohorts}</option>
            {cohorts.data?.map((cohort) => (
              <option key={cohort.id} value={cohort.id}>
                {cohort.name}
              </option>
            ))}
          </select>
        </label>
        <Button type="submit">{strings.adminSearch}</Button>
      </form>
      <QueryState isPending={students.isPending} error={students.error} onRetry={() => void students.refetch()}>
        {students.data && students.data.students.length === 0 ? (
          <p className="text-sm text-muted">{strings.adminNoStudents}</p>
        ) : (
          <Card className="overflow-x-auto p-0">
            <table className="w-full border-collapse text-sm">
              <thead>
                <tr className="border-b border-line text-left text-xs font-semibold uppercase tracking-[0.12em] text-muted">
                  <th className="px-5 py-3 font-semibold">{strings.adminStudents}</th>
                  <th className="px-5 py-3 font-semibold">{strings.adminCohorts}</th>
                  <th className="px-5 py-3 font-semibold">{strings.adminLastLogin}</th>
                  <th className="px-5 py-3 font-semibold">{strings.adminProgress}</th>
                </tr>
              </thead>
              <tbody>
                {students.data?.students.map((student) => (
                  <tr key={student.userId} className="border-b border-line/70 last:border-b-0">
                    <td className="px-5 py-3">
                      <Link to={`/admin/students/${student.userId}`} className="font-semibold hover:text-accent">
                        {student.displayName}
                      </Link>
                      <p className="text-xs text-muted">{student.email}</p>
                    </td>
                    <td className="px-5 py-3">{student.cohortName ?? strings.adminNoCohort}</td>
                    <td className="px-5 py-3 tabular">{formatDateTime(student.lastLoginAt)}</td>
                    <td className="px-5 py-3 tabular">
                      {student.completedLevelCount}/{student.totalLevelCount}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </Card>
        )}
        {students.data && students.data.totalCount > students.data.pageSize && (
          <div className="mt-4 flex items-center gap-3 text-sm">
            <Button variant="ghost" disabled={page <= 1} onClick={() => setPage((current) => current - 1)}>
              {strings.back}
            </Button>
            <span className="text-muted">
              {strings.adminPage} {page} {strings.of} {totalPages}
            </span>
            <Button variant="ghost" disabled={page >= totalPages} onClick={() => setPage((current) => current + 1)}>
              {strings.next}
            </Button>
          </div>
        )}
      </QueryState>
    </AdminLayout>
  );
}
