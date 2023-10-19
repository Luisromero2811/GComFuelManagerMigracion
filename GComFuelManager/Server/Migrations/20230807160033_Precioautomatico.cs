using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GComFuelManager.Server.Migrations
{
    /// <inheritdoc />
    public partial class Precioautomatico : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"insert into AspNetRoles (Id, Name, NormalizedName)
            values('f7cedb12-9064-4d9a-ae98-3975be9c0566','Validador Precio Automatico','VALIDADOR PRECIO AUTOMATICO')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("delete AspNetRoles where Id = 'f7cedb12-9064-4d9a-ae98-3975be9c0566'");
        }
    }
}
