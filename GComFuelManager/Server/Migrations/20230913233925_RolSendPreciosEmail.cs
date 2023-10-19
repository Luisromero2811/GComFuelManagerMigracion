using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GComFuelManager.Server.Migrations
{
    /// <inheritdoc />
    public partial class RolSendPreciosEmail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"insert into AspNetRoles (Id, Name, NormalizedName)
                values('0f48e41b-3167-4d84-a848-794ca37daf93', 'Envio Precios', 'ENVIO PRECIOS')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("delete AspNetRoles where Id = '0f48e41b-3167-4d84-a848-794ca37daf93'");
        }
    }
}
