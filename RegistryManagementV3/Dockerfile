FROM mcr.microsoft.com/dotnet/core/sdk:3.0
COPY . /app
WORKDIR /app
RUN dotnet restore
RUN dotnet tool install -g dotnet-ef --version="3.0.0"
ENV PATH="$PATH:/root/.dotnet/tools"
RUN dotnet build
EXPOSE 80/tcp
RUN chmod +x ./entrypoint.sh
CMD /bin/bash ./entrypoint.sh
