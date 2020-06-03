﻿using FluentMigrator;

namespace WinTenBot.Migrations.MySql
{
    public class CreateTableAfk:Migration
    {
        private const string TableName = "afk";
        
        public override void Up()
        {
            if (Schema.Table(TableName).Exists()) return;

            Create.Table(TableName)
                .WithColumn("id").AsInt16().PrimaryKey().Identity()
                .WithColumn("user_id").AsString(15)
                .WithColumn("chat_id").AsString(30)
                .WithColumn("afk_reason").AsCustom("TEXT")
                .WithColumn("is_afk").AsBoolean().WithDefaultValue(0)
                .WithColumn("afk_start").AsDateTime().WithDefault(SystemMethods.CurrentDateTime)
                .WithColumn("afk_end").AsDateTime().WithDefault(SystemMethods.CurrentDateTime);
        }

        public override void Down()
        {
            throw new System.NotImplementedException();
        }
    }
}