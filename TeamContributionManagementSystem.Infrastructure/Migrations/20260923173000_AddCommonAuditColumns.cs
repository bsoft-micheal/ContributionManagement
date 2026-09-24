using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeamContributionManagementSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCommonAuditColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. contributions
            migrationBuilder.Sql("ALTER TABLE public.contributions ADD COLUMN IF NOT EXISTS is_active BOOLEAN NOT NULL DEFAULT TRUE;");
            migrationBuilder.Sql("ALTER TABLE public.contributions ADD COLUMN IF NOT EXISTS created_by VARCHAR(150) NULL;");
            migrationBuilder.Sql("ALTER TABLE public.contributions ADD COLUMN IF NOT EXISTS created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP;");
            migrationBuilder.Sql("ALTER TABLE public.contributions ADD COLUMN IF NOT EXISTS modified_by VARCHAR(150) NULL;");
            migrationBuilder.Sql("ALTER TABLE public.contributions ADD COLUMN IF NOT EXISTS modified_on TIMESTAMP WITH TIME ZONE NULL;");

            // 2. device_details
            migrationBuilder.Sql("ALTER TABLE public.device_details ADD COLUMN IF NOT EXISTS is_deleted BOOLEAN NOT NULL DEFAULT FALSE;");
            migrationBuilder.Sql("ALTER TABLE public.device_details ADD COLUMN IF NOT EXISTS created_by VARCHAR(150) NULL;");
            migrationBuilder.Sql("ALTER TABLE public.device_details ADD COLUMN IF NOT EXISTS modified_by VARCHAR(150) NULL;");
            migrationBuilder.Sql("ALTER TABLE public.device_details ADD COLUMN IF NOT EXISTS modified_on TIMESTAMP WITH TIME ZONE NULL;");

            // 3. device_login_history
            migrationBuilder.Sql("ALTER TABLE public.device_login_history ADD COLUMN IF NOT EXISTS is_deleted BOOLEAN NOT NULL DEFAULT FALSE;");
            migrationBuilder.Sql("ALTER TABLE public.device_login_history ADD COLUMN IF NOT EXISTS created_by VARCHAR(150) NULL;");
            migrationBuilder.Sql("ALTER TABLE public.device_login_history ADD COLUMN IF NOT EXISTS created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP;");
            migrationBuilder.Sql("ALTER TABLE public.device_login_history ADD COLUMN IF NOT EXISTS modified_by VARCHAR(150) NULL;");
            migrationBuilder.Sql("ALTER TABLE public.device_login_history ADD COLUMN IF NOT EXISTS modified_on TIMESTAMP WITH TIME ZONE NULL;");

            // 4. event_participants
            migrationBuilder.Sql("ALTER TABLE public.event_participants ADD COLUMN IF NOT EXISTS is_active BOOLEAN NOT NULL DEFAULT TRUE;");
            migrationBuilder.Sql("ALTER TABLE public.event_participants ADD COLUMN IF NOT EXISTS is_deleted BOOLEAN NOT NULL DEFAULT FALSE;");
            migrationBuilder.Sql("ALTER TABLE public.event_participants ADD COLUMN IF NOT EXISTS created_by VARCHAR(150) NULL;");
            migrationBuilder.Sql("ALTER TABLE public.event_participants ADD COLUMN IF NOT EXISTS created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP;");
            migrationBuilder.Sql("ALTER TABLE public.event_participants ADD COLUMN IF NOT EXISTS modified_by VARCHAR(150) NULL;");
            migrationBuilder.Sql("ALTER TABLE public.event_participants ADD COLUMN IF NOT EXISTS modified_on TIMESTAMP WITH TIME ZONE NULL;");

            // 5. event_types
            migrationBuilder.Sql("ALTER TABLE public.event_types ADD COLUMN IF NOT EXISTS is_deleted BOOLEAN NOT NULL DEFAULT FALSE;");
            migrationBuilder.Sql("ALTER TABLE public.event_types ADD COLUMN IF NOT EXISTS created_by VARCHAR(150) NULL;");
            migrationBuilder.Sql("ALTER TABLE public.event_types ADD COLUMN IF NOT EXISTS created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP;");
            migrationBuilder.Sql("ALTER TABLE public.event_types ADD COLUMN IF NOT EXISTS modified_by VARCHAR(150) NULL;");
            migrationBuilder.Sql("ALTER TABLE public.event_types ADD COLUMN IF NOT EXISTS modified_on TIMESTAMP WITH TIME ZONE NULL;");

            // 6. events
            migrationBuilder.Sql("ALTER TABLE public.events ADD COLUMN IF NOT EXISTS is_active BOOLEAN NOT NULL DEFAULT TRUE;");
            migrationBuilder.Sql("ALTER TABLE public.events ADD COLUMN IF NOT EXISTS created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP;");
            migrationBuilder.Sql("ALTER TABLE public.events ADD COLUMN IF NOT EXISTS modified_by VARCHAR(150) NULL;");
            migrationBuilder.Sql("ALTER TABLE public.events ADD COLUMN IF NOT EXISTS modified_on TIMESTAMP WITH TIME ZONE NULL;");

            // 7. expenses
            migrationBuilder.Sql("ALTER TABLE public.expenses ADD COLUMN IF NOT EXISTS created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP;");

            // 8. gallery_photos
            migrationBuilder.Sql("ALTER TABLE public.gallery_photos ADD COLUMN IF NOT EXISTS created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP;");

            // 9. members
            migrationBuilder.Sql("ALTER TABLE public.members ADD COLUMN IF NOT EXISTS created_by VARCHAR(150) NULL;");
            migrationBuilder.Sql("ALTER TABLE public.members ADD COLUMN IF NOT EXISTS created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP;");
            migrationBuilder.Sql("ALTER TABLE public.members ADD COLUMN IF NOT EXISTS modified_by VARCHAR(150) NULL;");
            migrationBuilder.Sql("ALTER TABLE public.members ADD COLUMN IF NOT EXISTS modified_on TIMESTAMP WITH TIME ZONE NULL;");

            // 10. payment_transactions
            migrationBuilder.Sql("ALTER TABLE public.payment_transactions ADD COLUMN IF NOT EXISTS created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP;");

            // 11. role_rights
            migrationBuilder.Sql("ALTER TABLE public.role_rights ADD COLUMN IF NOT EXISTS is_active BOOLEAN NOT NULL DEFAULT TRUE;");
            migrationBuilder.Sql("ALTER TABLE public.role_rights ADD COLUMN IF NOT EXISTS is_deleted BOOLEAN NOT NULL DEFAULT FALSE;");
            migrationBuilder.Sql("ALTER TABLE public.role_rights ADD COLUMN IF NOT EXISTS created_by VARCHAR(150) NULL;");
            migrationBuilder.Sql("ALTER TABLE public.role_rights ADD COLUMN IF NOT EXISTS created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP;");
            migrationBuilder.Sql("ALTER TABLE public.role_rights ADD COLUMN IF NOT EXISTS modified_by VARCHAR(150) NULL;");
            migrationBuilder.Sql("ALTER TABLE public.role_rights ADD COLUMN IF NOT EXISTS modified_on TIMESTAMP WITH TIME ZONE NULL;");

            // 12. roles
            migrationBuilder.Sql("ALTER TABLE public.roles ADD COLUMN IF NOT EXISTS is_active BOOLEAN NOT NULL DEFAULT TRUE;");
            migrationBuilder.Sql("ALTER TABLE public.roles ADD COLUMN IF NOT EXISTS is_deleted BOOLEAN NOT NULL DEFAULT FALSE;");
            migrationBuilder.Sql("ALTER TABLE public.roles ADD COLUMN IF NOT EXISTS created_by VARCHAR(150) NULL;");
            migrationBuilder.Sql("ALTER TABLE public.roles ADD COLUMN IF NOT EXISTS created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP;");
            migrationBuilder.Sql("ALTER TABLE public.roles ADD COLUMN IF NOT EXISTS modified_by VARCHAR(150) NULL;");
            migrationBuilder.Sql("ALTER TABLE public.roles ADD COLUMN IF NOT EXISTS modified_on TIMESTAMP WITH TIME ZONE NULL;");

            // 13. support_tickets
            migrationBuilder.Sql("ALTER TABLE public.support_tickets ADD COLUMN IF NOT EXISTS created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP;");

            // 14. system_settings
            migrationBuilder.Sql("ALTER TABLE public.system_settings ADD COLUMN IF NOT EXISTS created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP;");

            // 15. user_mfa_devices
            migrationBuilder.Sql("ALTER TABLE public.user_mfa_devices ADD COLUMN IF NOT EXISTS is_active BOOLEAN NOT NULL DEFAULT TRUE;");
            migrationBuilder.Sql("ALTER TABLE public.user_mfa_devices ADD COLUMN IF NOT EXISTS is_deleted BOOLEAN NOT NULL DEFAULT FALSE;");
            migrationBuilder.Sql("ALTER TABLE public.user_mfa_devices ADD COLUMN IF NOT EXISTS created_by VARCHAR(150) NULL;");
            migrationBuilder.Sql("ALTER TABLE public.user_mfa_devices ADD COLUMN IF NOT EXISTS created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP;");
            migrationBuilder.Sql("ALTER TABLE public.user_mfa_devices ADD COLUMN IF NOT EXISTS modified_by VARCHAR(150) NULL;");
            migrationBuilder.Sql("ALTER TABLE public.user_mfa_devices ADD COLUMN IF NOT EXISTS modified_on TIMESTAMP WITH TIME ZONE NULL;");

            // 16. users
            migrationBuilder.Sql("ALTER TABLE public.users ADD COLUMN IF NOT EXISTS is_deleted BOOLEAN NOT NULL DEFAULT FALSE;");
            migrationBuilder.Sql("ALTER TABLE public.users ADD COLUMN IF NOT EXISTS created_by VARCHAR(150) NULL;");
            migrationBuilder.Sql("ALTER TABLE public.users ADD COLUMN IF NOT EXISTS created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP;");
            migrationBuilder.Sql("ALTER TABLE public.users ADD COLUMN IF NOT EXISTS modified_by VARCHAR(150) NULL;");
            migrationBuilder.Sql("ALTER TABLE public.users ADD COLUMN IF NOT EXISTS modified_on TIMESTAMP WITH TIME ZONE NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
