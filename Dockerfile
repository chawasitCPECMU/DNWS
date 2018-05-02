FROM microsoft/dotnet:2.0-sdk AS build-env
WORKDIR /app

COPY *.csproj ./
RUN dotnet restore
COPY . ./
RUN dotnet publish -c Release -r linux-x64 -o out
#RUN dotnet publish -c Release -o out

FROM microsoft/dotnet:2.0-runtime-deps
WORKDIR /app
COPY --from=build-env /app/out ./
#COPY /app/out ./
COPY ./config.json /app/out
COPY ./index.html /app/out
ENTRYPOINT [ "/app/DNWS" ]