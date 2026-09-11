using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CriticalListeningLab.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cohorts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cohorts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "modules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_enabled_globally = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_modules", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    entra_object_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    role = table.Column<int>(type: "integer", nullable: false),
                    cohort_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_login_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                    table.ForeignKey(
                        name: "fk_users_cohorts_cohort_id",
                        column: x => x.cohort_id,
                        principalTable: "cohorts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "audio_sources",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    module_id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audio_sources", x => x.id);
                    table.ForeignKey(
                        name: "fk_audio_sources_modules_module_id",
                        column: x => x.module_id,
                        principalTable: "modules",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cohort_module_availabilities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    cohort_id = table.Column<Guid>(type: "uuid", nullable: false),
                    module_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cohort_module_availabilities", x => x.id);
                    table.ForeignKey(
                        name: "fk_cohort_module_availabilities_cohorts_cohort_id",
                        column: x => x.cohort_id,
                        principalTable: "cohorts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_cohort_module_availabilities_modules_module_id",
                        column: x => x.module_id,
                        principalTable: "modules",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "exercise_segments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    module_id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_exercise_segments", x => x.id);
                    table.ForeignKey(
                        name: "fk_exercise_segments_modules_module_id",
                        column: x => x.module_id,
                        principalTable: "modules",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "audio_assets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    audio_source_id = table.Column<Guid>(type: "uuid", nullable: false),
                    variant_slug = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    storage_key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    mime_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    duration_ms = table.Column<int>(type: "integer", nullable: false),
                    sample_rate = table.Column<int>(type: "integer", nullable: false),
                    integrated_lufs = table.Column<double>(type: "double precision", nullable: true),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audio_assets", x => x.id);
                    table.ForeignKey(
                        name: "fk_audio_assets_audio_sources_audio_source_id",
                        column: x => x.audio_source_id,
                        principalTable: "audio_sources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "exercise_levels",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    segment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    level_number = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    exercise_type = table.Column<int>(type: "integer", nullable: false),
                    config_json = table.Column<string>(type: "jsonb", nullable: false),
                    question_count = table.Column<int>(type: "integer", nullable: false),
                    pass_threshold = table.Column<int>(type: "integer", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_exercise_levels", x => x.id);
                    table.ForeignKey(
                        name: "fk_exercise_levels_exercise_segments_segment_id",
                        column: x => x.segment_id,
                        principalTable: "exercise_segments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "level_unlock_requirements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    exercise_level_id = table.Column<Guid>(type: "uuid", nullable: false),
                    required_exercise_level_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requirement_type = table.Column<int>(type: "integer", nullable: false),
                    min_score = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_level_unlock_requirements", x => x.id);
                    table.CheckConstraint("ck_level_unlock_requirement_not_self", "exercise_level_id <> required_exercise_level_id");
                    table.ForeignKey(
                        name: "fk_level_unlock_requirements_exercise_levels_exercise_level_id",
                        column: x => x.exercise_level_id,
                        principalTable: "exercise_levels",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_level_unlock_requirements_exercise_levels_required_exercise",
                        column: x => x.required_exercise_level_id,
                        principalTable: "exercise_levels",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "student_progress",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    audio_source_id = table.Column<Guid>(type: "uuid", nullable: false),
                    exercise_level_id = table.Column<Guid>(type: "uuid", nullable: false),
                    best_score = table.Column<int>(type: "integer", nullable: false),
                    is_passed = table.Column<bool>(type: "boolean", nullable: false),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    first_passed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_attempt_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_student_progress", x => x.id);
                    table.ForeignKey(
                        name: "fk_student_progress_audio_sources_audio_source_id",
                        column: x => x.audio_source_id,
                        principalTable: "audio_sources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_student_progress_exercise_levels_exercise_level_id",
                        column: x => x.exercise_level_id,
                        principalTable: "exercise_levels",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_student_progress_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "test_sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    audio_source_id = table.Column<Guid>(type: "uuid", nullable: false),
                    exercise_level_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    question_count = table.Column<int>(type: "integer", nullable: false),
                    pass_threshold = table.Column<int>(type: "integer", nullable: false),
                    correct_answers = table.Column<int>(type: "integer", nullable: false),
                    passed = table.Column<bool>(type: "boolean", nullable: false),
                    random_seed = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_test_sessions", x => x.id);
                    table.ForeignKey(
                        name: "fk_test_sessions_audio_sources_audio_source_id",
                        column: x => x.audio_source_id,
                        principalTable: "audio_sources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_test_sessions_exercise_levels_exercise_level_id",
                        column: x => x.exercise_level_id,
                        principalTable: "exercise_levels",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_test_sessions_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "test_session_questions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    test_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    question_index = table.Column<int>(type: "integer", nullable: false),
                    prompt_json = table.Column<string>(type: "jsonb", nullable: false),
                    correct_answer_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    audio_asset_id = table.Column<Guid>(type: "uuid", nullable: true),
                    audio_token = table.Column<Guid>(type: "uuid", nullable: false),
                    student_answer_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    is_correct = table.Column<bool>(type: "boolean", nullable: true),
                    answered_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_test_session_questions", x => x.id);
                    table.ForeignKey(
                        name: "fk_test_session_questions_audio_assets_audio_asset_id",
                        column: x => x.audio_asset_id,
                        principalTable: "audio_assets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_test_session_questions_test_sessions_test_session_id",
                        column: x => x.test_session_id,
                        principalTable: "test_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_audio_assets_audio_source_id_variant_slug",
                table: "audio_assets",
                columns: new[] { "audio_source_id", "variant_slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_audio_sources_module_id_slug",
                table: "audio_sources",
                columns: new[] { "module_id", "slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_cohort_module_availabilities_cohort_id_module_id",
                table: "cohort_module_availabilities",
                columns: new[] { "cohort_id", "module_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_cohort_module_availabilities_module_id",
                table: "cohort_module_availabilities",
                column: "module_id");

            migrationBuilder.CreateIndex(
                name: "ix_cohorts_is_active",
                table: "cohorts",
                column: "is_active",
                unique: true,
                filter: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_cohorts_name",
                table: "cohorts",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_exercise_levels_segment_id_level_number",
                table: "exercise_levels",
                columns: new[] { "segment_id", "level_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_exercise_segments_module_id_key",
                table: "exercise_segments",
                columns: new[] { "module_id", "key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_level_unlock_requirements_exercise_level_id_required_exerci",
                table: "level_unlock_requirements",
                columns: new[] { "exercise_level_id", "required_exercise_level_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_level_unlock_requirements_required_exercise_level_id",
                table: "level_unlock_requirements",
                column: "required_exercise_level_id");

            migrationBuilder.CreateIndex(
                name: "ix_modules_slug",
                table: "modules",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_student_progress_audio_source_id",
                table: "student_progress",
                column: "audio_source_id");

            migrationBuilder.CreateIndex(
                name: "ix_student_progress_exercise_level_id",
                table: "student_progress",
                column: "exercise_level_id");

            migrationBuilder.CreateIndex(
                name: "ix_student_progress_user_id_audio_source_id",
                table: "student_progress",
                columns: new[] { "user_id", "audio_source_id" });

            migrationBuilder.CreateIndex(
                name: "ix_student_progress_user_id_audio_source_id_exercise_level_id",
                table: "student_progress",
                columns: new[] { "user_id", "audio_source_id", "exercise_level_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_test_session_questions_audio_asset_id",
                table: "test_session_questions",
                column: "audio_asset_id");

            migrationBuilder.CreateIndex(
                name: "ix_test_session_questions_audio_token",
                table: "test_session_questions",
                column: "audio_token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_test_session_questions_test_session_id_question_index",
                table: "test_session_questions",
                columns: new[] { "test_session_id", "question_index" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_test_sessions_audio_source_id",
                table: "test_sessions",
                column: "audio_source_id");

            migrationBuilder.CreateIndex(
                name: "ix_test_sessions_exercise_level_id",
                table: "test_sessions",
                column: "exercise_level_id");

            migrationBuilder.CreateIndex(
                name: "ix_test_sessions_user_id_exercise_level_id_audio_source_id_sta",
                table: "test_sessions",
                columns: new[] { "user_id", "exercise_level_id", "audio_source_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_users_cohort_id",
                table: "users",
                column: "cohort_id");

            migrationBuilder.CreateIndex(
                name: "ix_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_entra_object_id",
                table: "users",
                column: "entra_object_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cohort_module_availabilities");

            migrationBuilder.DropTable(
                name: "level_unlock_requirements");

            migrationBuilder.DropTable(
                name: "student_progress");

            migrationBuilder.DropTable(
                name: "test_session_questions");

            migrationBuilder.DropTable(
                name: "audio_assets");

            migrationBuilder.DropTable(
                name: "test_sessions");

            migrationBuilder.DropTable(
                name: "audio_sources");

            migrationBuilder.DropTable(
                name: "exercise_levels");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "exercise_segments");

            migrationBuilder.DropTable(
                name: "cohorts");

            migrationBuilder.DropTable(
                name: "modules");
        }
    }
}
