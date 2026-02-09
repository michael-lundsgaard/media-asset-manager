using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MediaAssetManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "clients",
                columns: table => new
                {
                    client_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    client_public_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    client_secret_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    client_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_clients", x => x.client_id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    user_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    username = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.user_id);
                });

            migrationBuilder.CreateTable(
                name: "media_assets",
                columns: table => new
                {
                    asset_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: true),
                    file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    original_file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    file_size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    media_type = table.Column<int>(type: "integer", nullable: false),
                    content_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    game_title = table.Column<string>(type: "text", nullable: true),
                    tags = table.Column<List<string>>(type: "jsonb", nullable: false),
                    storage_path = table.Column<string>(type: "text", nullable: false),
                    thumbnail_path = table.Column<string>(type: "text", nullable: true),
                    is_compressed = table.Column<bool>(type: "boolean", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    processing_error = table.Column<string>(type: "text", nullable: true),
                    lifecycle = table.Column<int>(type: "integer", nullable: false),
                    is_public = table.Column<bool>(type: "boolean", nullable: false),
                    uploaded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_viewed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    view_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_media_assets", x => x.asset_id);
                    table.ForeignKey(
                        name: "fk_media_assets_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "playlists",
                columns: table => new
                {
                    playlist_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_public = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_playlists", x => x.playlist_id);
                    table.ForeignKey(
                        name: "fk_playlists_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "asset_views",
                columns: table => new
                {
                    view_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    asset_id = table.Column<int>(type: "integer", nullable: false),
                    user_id = table.Column<int>(type: "integer", nullable: true),
                    viewed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asset_views", x => x.view_id);
                    table.ForeignKey(
                        name: "fk_asset_views_media_assets_asset_id",
                        column: x => x.asset_id,
                        principalTable: "media_assets",
                        principalColumn: "asset_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_asset_views_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "favorites",
                columns: table => new
                {
                    favorite_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    asset_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_favorites", x => x.favorite_id);
                    table.ForeignKey(
                        name: "fk_favorites_media_assets_asset_id",
                        column: x => x.asset_id,
                        principalTable: "media_assets",
                        principalColumn: "asset_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_favorites_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "video_metadata",
                columns: table => new
                {
                    video_metadata_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    asset_id = table.Column<int>(type: "integer", nullable: false),
                    duration_seconds = table.Column<int>(type: "integer", nullable: false),
                    width = table.Column<int>(type: "integer", nullable: false),
                    height = table.Column<int>(type: "integer", nullable: false),
                    frame_rate = table.Column<decimal>(type: "numeric", nullable: false),
                    codec = table.Column<string>(type: "text", nullable: true),
                    bitrate_kbps = table.Column<int>(type: "integer", nullable: true),
                    audio_codec = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_video_metadata", x => x.video_metadata_id);
                    table.ForeignKey(
                        name: "fk_video_metadata_media_assets_asset_id",
                        column: x => x.asset_id,
                        principalTable: "media_assets",
                        principalColumn: "asset_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "playlist_items",
                columns: table => new
                {
                    playlist_item_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    playlist_id = table.Column<int>(type: "integer", nullable: false),
                    asset_id = table.Column<int>(type: "integer", nullable: false),
                    added_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_playlist_items", x => x.playlist_item_id);
                    table.ForeignKey(
                        name: "fk_playlist_items_media_assets_asset_id",
                        column: x => x.asset_id,
                        principalTable: "media_assets",
                        principalColumn: "asset_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_playlist_items_playlists_playlist_id",
                        column: x => x.playlist_id,
                        principalTable: "playlists",
                        principalColumn: "playlist_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_asset_views_asset_id_viewed_at",
                table: "asset_views",
                columns: new[] { "asset_id", "viewed_at" });

            migrationBuilder.CreateIndex(
                name: "ix_asset_views_user_id",
                table: "asset_views",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_asset_views_viewed_at",
                table: "asset_views",
                column: "viewed_at");

            migrationBuilder.CreateIndex(
                name: "ix_clients_client_public_id",
                table: "clients",
                column: "client_public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_clients_is_active",
                table: "clients",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_favorites_asset_id",
                table: "favorites",
                column: "asset_id");

            migrationBuilder.CreateIndex(
                name: "ix_favorites_created_at",
                table: "favorites",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_favorites_user_id_asset_id",
                table: "favorites",
                columns: new[] { "user_id", "asset_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_media_assets_content_hash",
                table: "media_assets",
                column: "content_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_media_assets_game_title",
                table: "media_assets",
                column: "game_title");

            migrationBuilder.CreateIndex(
                name: "ix_media_assets_is_public_user_id",
                table: "media_assets",
                columns: new[] { "is_public", "user_id" });

            migrationBuilder.CreateIndex(
                name: "ix_media_assets_media_type",
                table: "media_assets",
                column: "media_type");

            migrationBuilder.CreateIndex(
                name: "ix_media_assets_uploaded_at",
                table: "media_assets",
                column: "uploaded_at");

            migrationBuilder.CreateIndex(
                name: "ix_media_assets_user_id",
                table: "media_assets",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_playlist_items_asset_id",
                table: "playlist_items",
                column: "asset_id");

            migrationBuilder.CreateIndex(
                name: "ix_playlist_items_playlist_id_asset_id",
                table: "playlist_items",
                columns: new[] { "playlist_id", "asset_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_playlists_user_id",
                table: "playlists",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_playlists_user_id_is_public",
                table: "playlists",
                columns: new[] { "user_id", "is_public" });

            migrationBuilder.CreateIndex(
                name: "ix_users_username",
                table: "users",
                column: "username");

            migrationBuilder.CreateIndex(
                name: "ix_video_metadata_asset_id",
                table: "video_metadata",
                column: "asset_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_video_metadata_width_height",
                table: "video_metadata",
                columns: new[] { "width", "height" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "asset_views");

            migrationBuilder.DropTable(
                name: "clients");

            migrationBuilder.DropTable(
                name: "favorites");

            migrationBuilder.DropTable(
                name: "playlist_items");

            migrationBuilder.DropTable(
                name: "video_metadata");

            migrationBuilder.DropTable(
                name: "playlists");

            migrationBuilder.DropTable(
                name: "media_assets");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
