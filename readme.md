# Work Bot

A bot to help query and manage work items across **GitHub** and **Visual Studio Team Services**.

## Requirements

Download and install the following:

- **.NET Core SDK** from https://www.microsoft.com/net/core#windowscmd
- **Git for Windows** from https://git-scm.com/download/win
- **Bot Framework Emulator** from https://docs.botframework.com/en-us/tools/bot-framework-emulator/

You can get either:
- **Visual Studio Code** from https://code.visualstudio.com/
- **Visual Studio 2017 - Community** from https://www.visualstudio.com/vs/community/

Also some useful extensions for VSCode include:

- C#
- C# Extensions
- GitLens
- NuGet Package Manager

## Getting started

Clone the repository using:

```cmd
git clone https://github.com/WrathOfZombies/workbot
```

After cloning the repository run the following to download and install all the required dependencies:

```cmd
dotnet restore
```

## Running locally

Create an `appsettings.json` file in the **root** of the project and paste the following into it:

```json
{
    "Logging": {
        "IncludeScopes": false,
        "LogLevel": {
            "Default": "Warning"
        }
    },
    "BotCredentials": {
        "ClientId": "CLIENT_ID",
        "ClientSecret": "CLIENT_SECRET"
    }
}
```

> Note: If you are a part of the core dev group, you'll be able to find the CLIENT_ID and CLIENT_SECRET in **Teams**.

Use `F5` to start the project.

> Note: If you are having problems debugging on **VSCode** then type `Ctrl` + `Shift` + `P` and enter **Debug: Download .NET Core Debugger** to download the debugger.

### Using Visual Studio Code 

Navigate to `http://localhost:5000/api/echo` and you should see a message such as *Echo bot is ready with <CLIENT_ID>*

### Using Visual Studio

Navigate to `https://localhost:44300/api/echo`. If required make sure to trust the certificates. Follow the following gif to do the same: [Trusting SSL Cert on Windows](https://github.com/OfficeDev/script-lab/blob/master/.github/images/trust-ssl-internet-explorer.gif)

## Deploying to Azure

### Using Visual Studio

Everything is configured to work with Visual Studio out of the box. Hence just **Right Click** on the **solution** and click `Publish` and it should just deploy.

> Note: For the first time you'll be asked to login in using your deployment credentials.

If it doesn't work then follow the manual deploy steps or file an issue.

### Manual steps

Follow the steps to deploy to Azure manually.
> Note: This assumes that you have permissions and have already set your deployment credentials on Azure. If not please complete those steps first.

For the first time, do the following:

```cmd
git remote add azure https://username@work-bot.scm.azurewebsites.net:443/work-bot.git
```

Subsequently you can do:

```cmd
git push azure master:master
```

This reads as `Deploy to 'azure' from 'master' branch to (:) 'master' branch`.
Azure automatically detects the presence of `WorkBot.Entry.csproj` and resorts to download and building a **.NET Core** project.

## Helpful commands

### Git

To clear all local changes and create a fresh build use the following commands:

**For a soft clean:**
```cmd
git clean -fdx
dotnet restore
```

**For a complete clean:**
```cmd
git reset HEAD --hard
git clean -fdx
dotnet restore
```

### VSCode

In VSCode the following commands can be useful (**assumes Windows**):

- `Ctrl` + `Shift` + `B` ==> Builds the project.
- `F5` ==> Starts the web server.
- `Shift` + `Alt` + `F` ==> Format the document.
- `Ctrl` + `P` ==> Open a file in the project.
- `Ctrl` + `Shift` + `O` ==> Search for a symbol ie. Class, Variable, Function, Property etc.
- `Ctrl` + `~` ==> Open integrated terminal.
- `Ctrl` + `Shift` + `V` ==> Preview Markdown files.

## Useful links

- https://code.visualstudio.com/docs/other/dotnet
- https://docs.microsoft.com/en-us/aspnet/core/tutorials/web-api-vsc
- https://www.hanselman.com/blog/ExploringTheNewNETDotnetCommandLineInterfaceCLI.aspx
- https://carlos.mendible.com/2016/09/11/netcore-and-microsoft-bot-framework/
- https://github.com/CXuesong/BotBuilder.Standard/blob/portable/CSharp/README_PORTABLE.md
- https://github.com/CXuesong/BotBuilder.Standard/tree/portable/CSharp/Samples