﻿using System;
using System.Threading.Tasks;
using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(SandboxDatabaseManager.Startup))]
namespace SandboxDatabaseManager
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // Any connection or hub wire up and configuration should go here
           app.MapSignalR();

        }
    }
}


