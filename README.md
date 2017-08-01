# SQLServerScripter

Thanks to .NET Core it is finally possible to generate SQL Server databases scripts from Linux (by using C#).
For such tasks one usually had to use the SQL Server Management Studio (SSMS) from Microsoft - which doesn't run on Linux:
![ SQL Server Management Studio Context Menu](/images/ssms-right-click.png)
![ SQL Server Management Studio Advanced Scripting Options](/images/ssms-advanced-scripting-options.png)

## Setup

### Prerequisite

`SQLServerScripter` needs to run on `.Net Core 2.0`. As at the time of writing this docs `.Net Core 2.0` is in preview (July 2017).

### Install .Net Core and

First [install Microsoft .Net Core](https://www.microsoft.com/net/core).

I recommend using the Docker image:

```bash
sudo docker pull microsoft/dotnet:2.0.0-preview2-sdk
```

### Get the code

Now clone this GitHub repository.
```bash
git clone git@github.com:mkurz/SQLServerScripter.git
```

Let's assume you cloned it to the folder `/path/to/SQLServerScripter/`.

## Usage

Let's say you want a script to be saved in the folder `/path/to/output/folder/` then you start a docker container like this:

```bash
sudo docker run -it -v /path/to/SQLServerScripter/:/SQLServerScripter -v /path/to/output/folder/:/SQLScripts --rm microsoft/dotnet:2.0.0-preview2-sdk
```

Inside the container you can now run the `SQLServerScripter` inside the `/SQLServerScripter/` folder by calling:
```bash
dotnet run <datasource> <database> <username> <outputFolder>
```
The program will then prompt you for the database password.
(The first run may take a couple of seconds because some dependencies have to be evaluated first)

For example:

```bash
cd /SQLServerScripter/
# Script the SQL Server database "Northwind" with user "Einstein" running on another docker container or the host via IP on port 1436:
dotnet run 172.17.0.5,1436 Northwind Einstein /SQLScripts/
# Script an Azure SQL Server database "Westwind" with user "DaVinci" running in the Azure cloud:
dotnet run someAzureDomain.database.windows.net Westwind DaVinci /SQLScripts/
```

## Modify the code

If you have a look in the source code you will recognize that you can easily change the parameters for the generated script by changing the `ScriptingOptions` properties.

Just FYI (but probably not relevant for you anyway):
The NuGet packages you see in `/bin/nuget` were copied over from [Microsoft's SQL Tools Service](https://github.com/Microsoft/sqltoolsservice/tree/7ef81d0e5409dab6ba999b21f5214fc43bd0f08c/bin/nuget) - which is (also) used in [Visual Studio Code mssql plugin](https://marketplace.visualstudio.com/items?itemName=ms-mssql.mssql).

## Hint

The generated script should be `UTF-8` encoded. However it seems some comments, etc. use Windows line breaks. You can convert the sql script to a unix file by running:

```bash
dos2unix myscript.sql -o myscript.sql # replaces the original file
```
