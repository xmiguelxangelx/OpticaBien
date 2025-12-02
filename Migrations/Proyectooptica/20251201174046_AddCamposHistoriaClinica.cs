using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Optica1.Migrations.Proyectooptica
{
    /// <inheritdoc />
    public partial class AddCamposHistoriaClinica : Migration
    {
        /// <inheritdoc />
        
            protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IdUsuarioPaciente",
                table: "historiaclinica",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IdUsuarioOptometra",
                table: "historiaclinica",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MotivoConsulta",
                table: "historiaclinica",
                type: "longtext",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Antecedentes",
                table: "historiaclinica",
                type: "longtext",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AgudezaVisualOd",
                table: "historiaclinica",
                type: "varchar(50)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AgudezaVisualOi",
                table: "historiaclinica",
                type: "varchar(50)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RxFinalOd",
                table: "historiaclinica",
                type: "varchar(100)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RxFinalOi",
                table: "historiaclinica",
                type: "varchar(100)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Observaciones",
                table: "historiaclinica",
                type: "longtext",
                nullable: true);
        }

                    

        /// <inheritdoc />
       
           protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IdUsuarioPaciente",
                table: "historiaclinica");

            migrationBuilder.DropColumn(
                name: "IdUsuarioOptometra",
                table: "historiaclinica");

            migrationBuilder.DropColumn(
                name: "MotivoConsulta",
                table: "historiaclinica");

            migrationBuilder.DropColumn(
                name: "Antecedentes",
                table: "historiaclinica");

            migrationBuilder.DropColumn(
                name: "AgudezaVisualOd",
                table: "historiaclinica");

            migrationBuilder.DropColumn(
                name: "AgudezaVisualOi",
                table: "historiaclinica");

            migrationBuilder.DropColumn(
                name: "RxFinalOd",
                table: "historiaclinica");

            migrationBuilder.DropColumn(
                name: "RxFinalOi",
                table: "historiaclinica");

            migrationBuilder.DropColumn(
                name: "Observaciones",
                table: "historiaclinica");
        }

    }
}

