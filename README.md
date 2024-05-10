# Coffee-machine-API

## Issue tracking

[Kanban](https://github.com/users/Leo-Ma0502/projects/3)

## Basic requirements

Branch: [Main](https://github.com/Leo-Ma0502/Coffee-machine-API)

## Weather integration

Branch: [6-feature/integrate-temperature-information](https://github.com/Leo-Ma0502/Coffee-machine-API/tree/6-feature/integrate-temperature-information)

## Clone repository

```
git clone https://github.com/Leo-Ma0502/Coffee-machine-API.git

cd Coffee-machine-API
```

## Environment variables (IMPORTANT IF PLANNING TO RUN IT)

To secure the third party url and api key, these variables are not included in the source code, and the configuration file containing these information are removed from the repository. Thus, please create a file "appsettings.json" in Coffee-machine-API/CoffeeMachineAPI, and add the following content in it:

```
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ExternalApi":{
    "Url": {url},
    "ApiKey": {apikey}
  }
}
```

Please refer to [this google document](https://docs.google.com/document/d/1I8J_-wkqF0aPMc-QKPGjfvzD-IwRT988nDpXxIQSrb0/edit?usp=sharing) to get the value of url and apikey.

## How to test

```
dotnet test
```

## How to run

```
cd CoffeeMachineAPI

dotnet run
```
