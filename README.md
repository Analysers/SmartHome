# SmartHome

Simple smart home sensor / server.

## Server usage

1. Install Dotnet Core Runtime Version 2.2

2. Clone the Repo into your home server / NAS

3. In the repo folder, run the following command to generate a published server and a sqlite database

    ```bash
    cd SmartHome
    dotnet publish -c Release
    dotnet ef database update
    cp data.db bin/Release/netcoreapp2.2/publish/
    ```

4. Change settings in ```SmartHome/bin/Release/netcoreapp2.2/publish/appsettings.json``` if you want to enable telegram bot feature

5. You can now run your server in ```SmartHome/bin/Release/netcoreapp2.2/publish/``` with command ```dotnet SmartHome.dll```

## Sensor usage

- [TempSensor](https://github.com/arition/SmartHome/tree/master/TempSensor)
- IRRemote (Doc WIP)
- SmartLock (Doc WIP)
- SmartSwitch (Doc WIP)
