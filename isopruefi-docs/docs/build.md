# Contribute & Build 

## How do I contribute as a developer?
<p style="color: red;"><b>READ THIS GUIDE BEFORE CONTRIBUTING</b></p>

Since our project is secured by two pre-commit hocks, it is important to set up the project correctly before contributing.

This is done as followed:

Clone the project

```git clone https://github.com/deadmade/PGA-SE-KSTH.git```

Make sure you have installed the following packages globally.

- <a href="https://www.python.org/">Python</a>: Needed for MkDocs
- <a href="https://www.npmjs.com/">Node Package Manager</a>: Used to install needed dependencies for pre-commit hooks
- <a href="https://dotnet.microsoft.com/en-us/download">.NET 9.0 SDK</a>: Used for our Rest-API
- <a href="https://www.docker.com/">Docker</a>

After you've cloned the repo make sure to install all needed packages for the hooks via:

```npm i```

and run:

```npm run init```

Now it should be configured 🚀

To get the development environment up and running, follow these steps:

1. Open a terminal, navigate to the `IsoPrüfi` directory, and run:

   ```bash
   docker compose up
   ```

2. Once the containers are running, create an admin token for InfluxDB:

   ```bash
   docker exec -it influxdb influxdb3 create token --admin
   ```

3. Copy the generated token string.

4. Create a `config.json` file at the following location:

   ```
   IsoPruefi/isopruefi-docker/influx/explorer/config
   ```

5. Add the following content to `config.json`, replacing `"your-token-here"` with the copied token:

   ```json
   {
     "DEFAULT_INFLUX_SERVER": "http://host.docker.internal:8181",
     "DEFAULT_INFLUX_DATABASE": "IsoPrüfi",
     "DEFAULT_API_TOKEN": "your-token-here",
     "DEFAULT_SERVER_NAME": "IsoPrüfi"
   }
   ```
   
6. Run dotnet user-secrets set "Influx:InfluxDBToken" "<Token>" --project isopruefi-backend\MQTT-Receiver-Worker\MQTT-Receiver-Worker.csproj 

7. Restart the Containers

Happy Coding 😊
