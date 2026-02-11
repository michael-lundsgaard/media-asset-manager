using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MediaAssetManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddViewCountTrigger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "view_count",
                table: "media_assets",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer");

            // Drop existing trigger if it exists (for idempotency)
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS trigger_update_view_count ON asset_views;");

            // Create function to update view count automatically
            migrationBuilder.Sql(@"
                CREATE OR REPLACE FUNCTION update_asset_view_count()
                RETURNS TRIGGER AS $$
                BEGIN
                    IF TG_OP = 'INSERT' THEN
                        UPDATE media_assets
                        SET view_count = view_count + 1
                        WHERE asset_id = NEW.asset_id;
                    ELSIF TG_OP = 'DELETE' THEN
                        UPDATE media_assets
                        SET view_count = view_count - 1
                        WHERE asset_id = OLD.asset_id;
                    END IF;
                    RETURN NULL;
                END;
                $$ LANGUAGE plpgsql;
            ");

            // Create trigger that fires on INSERT/DELETE to asset_views table
            migrationBuilder.Sql(@"
                CREATE TRIGGER trigger_update_view_count
                AFTER INSERT OR DELETE ON asset_views
                FOR EACH ROW EXECUTE FUNCTION update_asset_view_count();
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop trigger and function in reverse order
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS trigger_update_view_count ON asset_views;");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS update_asset_view_count();");

            migrationBuilder.AlterColumn<int>(
                name: "view_count",
                table: "media_assets",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 0);
        }
    }
}
